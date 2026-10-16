#!/bin/sh
# Hide Copilot code reviews that no longer need attention.
#
# A Copilot review on a pull request is hidden on the Conversation page with the
# reason "Outdated" (the same as choosing ... > Hide > Outdated in GitHub) when:
#   - it has inline comments, and every review thread that holds one of them is
#     marked Outdated (resolved threads do not count); or
#   - it has no inline comments, and a newer Copilot review exists.
#
# Left alone: reviews by anyone other than Copilot, reviews that are already
# hidden, reviews with a thread that is not Outdated, and the newest Copilot
# review when it has no inline comments.
#
# Usage:
#   sh hide-outdated-copilot-reviews.sh OWNER/REPO PR_NUMBER [PR_NUMBER ...]
#   sh hide-outdated-copilot-reviews.sh OWNER/REPO --all-open
#
# Environment:
#   DRY_RUN=1       Report what would be hidden without hiding anything.
#   COPILOT_LOGINS  Space-separated review author logins treated as Copilot
#                   (case-insensitive). Default:
#                   copilot-pull-request-reviewer copilot-pull-request-reviewer[bot]
#
# Requires the GitHub CLI (gh), signed in as an account that can hide comments
# in the repository, or GH_TOKEN set when running in GitHub Actions.
# Exit status: 0 finished, 1 something could not be read or hidden, 2 usage error.

set -u
set -f  # no filename globbing, so the one unquoted expansion below never matches files

: "${COPILOT_LOGINS:=copilot-pull-request-reviewer copilot-pull-request-reviewer[bot]}"
TAB=$(printf '\t')

# The GraphQL, jq, and awk programs below are single-quoted on purpose: their
# $ variables belong to those languages, not to the shell.

# shellcheck disable=SC2016
REVIEWS_QUERY='
query($owner: String!, $repo: String!, $number: Int!, $endCursor: String) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $number) {
      reviews(first: 100, after: $endCursor) {
        pageInfo {
          hasNextPage
          endCursor
        }
        nodes {
          id
          url
          submittedAt
          isMinimized
          viewerCanMinimize
          author {
            login
          }
          comments(first: 1) {
            totalCount
          }
        }
      }
    }
  }
}'

# One line per review: R, id, author, isMinimized, viewerCanMinimize, URL,
# submittedAt, and its number of inline comments.
# shellcheck disable=SC2016
REVIEWS_JQ='
.data.repository.pullRequest.reviews.nodes[]
| ["R", .id, (.author.login // "ghost"), (.isMinimized | tostring),
   (.viewerCanMinimize | tostring), .url, (.submittedAt // "-"),
   (.comments.totalCount | tostring)]
| @tsv'

# shellcheck disable=SC2016
THREADS_QUERY='
query($owner: String!, $repo: String!, $number: Int!, $endCursor: String) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $number) {
      reviewThreads(first: 100, after: $endCursor) {
        pageInfo {
          hasNextPage
          endCursor
        }
        nodes {
          id
          isOutdated
          comments(first: 100) {
            totalCount
            nodes {
              pullRequestReview {
                id
              }
            }
          }
        }
      }
    }
  }
}'

# One line per (thread, review) pair: T, review id, thread isOutdated. A thread
# with more comments than were fetched prints X instead, so no review is hidden
# on that pull request because of its threads.
# shellcheck disable=SC2016
THREADS_JQ='
.data.repository.pullRequest.reviewThreads.nodes[]
| if .comments.totalCount > (.comments.nodes | length) then
    ["X", .id] | @tsv
  else
    (.isOutdated | tostring) as $outdated
    | [.comments.nodes[].pullRequestReview.id | select(. != null)]
    | unique[]
    | ["T", ., $outdated]
    | @tsv
  end'

# Reads the R lines, then the T and X lines, and prints one decision per Copilot
# review: HIDE, SKIP, or FAIL, then the review id, review URL, and a short reason.
# Reviews by others are summed up in a single NOTE line.
# shellcheck disable=SC2016
DECIDE_AWK='
BEGIN {
    FS = OFS = "\t"
    n = split(tolower(logins), names, " ")
    for (i = 1; i <= n; i++)
        if (names[i] != "") copilot[names[i]] = 1
    newest = ""
}
$1 == "R" {
    id = $2
    if (id in author) next
    order[++count] = id
    author[id] = $3; hidden[id] = $4; canhide[id] = $5; url[id] = $6
    submitted[id] = $7; comments[id] = $8 + 0; threads[id] = 0; outdated[id] = 0
    if ((tolower($3) in copilot) && $7 != "-" && $7 > newest) newest = $7
    next
}
$1 == "X" { truncated++; next }
$1 == "T" {
    threads[$2]++
    if ($3 == "true") outdated[$2]++
    next
}
END {
    for (k = 1; k <= count; k++) {
        id = order[k]
        who = author[id]
        if (!(tolower(who) in copilot)) {
            others_count++
            if (!(who in seen)) {
                seen[who] = 1
                if (others != "") others = others ", "
                others = others who
            }
            continue
        }
        if (comments[id] == 0) {
            detail = who ", no inline comments"
            if (hidden[id] == "true") { print "SKIP", id, url[id], detail ", already hidden"; continue }
            if (submitted[id] == "-" || !(submitted[id] < newest)) { print "SKIP", id, url[id], detail ", no newer Copilot review yet"; continue }
            detail = detail ", a newer Copilot review exists"
        } else {
            detail = who ", " outdated[id] " of " threads[id] " comment threads Outdated"
            if (hidden[id] == "true") { print "SKIP", id, url[id], detail ", already hidden"; continue }
            if (threads[id] == 0) { print "SKIP", id, url[id], who ", its inline comments were not found in any review thread"; continue }
            if (outdated[id] < threads[id]) { print "SKIP", id, url[id], detail; continue }
            if (truncated) { print "SKIP", id, url[id], detail ", but a thread on this pull request has more than 100 comments, so it is not hidden"; continue }
        }
        if (canhide[id] != "true")
            print "FAIL", id, url[id], detail ", but this account is not allowed to hide it (viewerCanMinimize is false)"
        else
            print "HIDE", id, url[id], detail
    }
    if (others_count) print "NOTE", "-", "-", others_count " review(s) by others (" others ")"
}'

# shellcheck disable=SC2016
MINIMIZE_MUTATION='
mutation($id: ID!) {
  minimizeComment(input: {subjectId: $id, classifier: OUTDATED}) {
    minimizedComment {
      isMinimized
      minimizedReason
    }
  }
}'

# shellcheck disable=SC2016
OPEN_PULL_REQUESTS_QUERY='
query($owner: String!, $repo: String!, $endCursor: String) {
  repository(owner: $owner, name: $repo) {
    pullRequests(states: OPEN, first: 100, after: $endCursor) {
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        number
      }
    }
  }
}'

usage() {
    printf 'usage: sh %s OWNER/REPO PR_NUMBER [PR_NUMBER ...]\n' "$0" >&2
    printf '       sh %s OWNER/REPO --all-open\n' "$0" >&2
    exit 2
}

failures=0
hidden_count=0
would_hide=0

check_pull_request() {
    pr=$1
    printf '%s#%s\n' "$repository" "$pr"

    if ! review_rows=$(gh api graphql --paginate \
            -f query="$REVIEWS_QUERY" \
            -f owner="$owner" -f repo="$name" -F number="$pr" \
            --jq "$REVIEWS_JQ" </dev/null); then
        printf '  error: could not read the reviews of %s#%s (see the gh message above)\n' "$repository" "$pr" >&2
        failures=$((failures + 1))
        return
    fi

    if ! thread_rows=$(gh api graphql --paginate \
            -f query="$THREADS_QUERY" \
            -f owner="$owner" -f repo="$name" -F number="$pr" \
            --jq "$THREADS_JQ" </dev/null); then
        printf '  error: could not read the review threads of %s#%s (see the gh message above)\n' "$repository" "$pr" >&2
        failures=$((failures + 1))
        return
    fi

    if ! decisions=$(printf '%s\n%s\n' "$review_rows" "$thread_rows" |
            awk -v logins="$COPILOT_LOGINS" "$DECIDE_AWK"); then
        printf '  error: awk could not process the reviews of %s#%s\n' "$repository" "$pr" >&2
        failures=$((failures + 1))
        return
    fi
    if [ -z "$decisions" ]; then
        printf '  no reviews yet\n'
        return
    fi

    while IFS=$TAB read -r action review_id review_url detail; do
        case $action in
            NOTE)
                printf '  left alone: %s\n' "$detail"
                ;;
            SKIP)
                printf '  left alone: %s (%s)\n' "$review_url" "$detail"
                ;;
            FAIL)
                printf '  error: %s (%s)\n' "$review_url" "$detail" >&2
                failures=$((failures + 1))
                ;;
            HIDE)
                if [ "$dry_run" = 1 ]; then
                    printf '  would hide: %s (%s)\n' "$review_url" "$detail"
                    would_hide=$((would_hide + 1))
                elif result=$(gh api graphql \
                        -f query="$MINIMIZE_MUTATION" -f id="$review_id" \
                        --jq '.data.minimizeComment.minimizedComment.isMinimized' </dev/null) &&
                        [ "$result" = true ]; then
                    printf '  hidden: %s (%s)\n' "$review_url" "$detail"
                    hidden_count=$((hidden_count + 1))
                    sleep 1  # GitHub asks for a pause between content-changing requests
                else
                    printf '  error: %s (GitHub did not confirm the review is hidden)\n' "$review_url" >&2
                    failures=$((failures + 1))
                fi
                ;;
            *)
                printf '  error: unexpected decision line: %s\n' "$action" >&2
                failures=$((failures + 1))
                ;;
        esac
    done <<EOF
$decisions
EOF
}

case ${DRY_RUN:-} in
    1 | true | TRUE | True | yes | YES) dry_run=1 ;;
    '' | 0 | false | FALSE | False | no | NO) dry_run=0 ;;
    *)
        printf 'error: DRY_RUN must be 1 or 0 (or true or false), not "%s"\n' "$DRY_RUN" >&2
        exit 2
        ;;
esac

[ "$#" -ge 2 ] || usage
repository=$1
shift
case $repository in
    */*/* | /* | */ | *[!A-Za-z0-9._/-]*) usage ;;
    */*) ;;
    *) usage ;;
esac
owner=${repository%%/*}
name=${repository#*/}

if [ "$1" = --all-open ]; then
    [ "$#" -eq 1 ] || usage
else
    for pr in "$@"; do
        case $pr in
            '' | *[!0-9]*) usage ;;
            *) ;;
        esac
    done
fi

if ! command -v gh >/dev/null 2>&1; then
    printf 'error: the GitHub CLI (gh) is not installed; see https://cli.github.com\n' >&2
    exit 1
fi

if [ "$dry_run" = 1 ]; then
    printf 'Dry run: nothing will be hidden.\n'
fi

if [ "$1" = --all-open ]; then
    if ! open_prs=$(gh api graphql --paginate \
            -f query="$OPEN_PULL_REQUESTS_QUERY" \
            -f owner="$owner" -f repo="$name" \
            --jq '.data.repository.pullRequests.nodes[].number' </dev/null); then
        printf 'error: could not list the open pull requests of %s (see the gh message above)\n' "$repository" >&2
        exit 1
    fi
    # Unquoted on purpose: one argument per pull request number (globbing is off).
    # shellcheck disable=SC2086
    set -- $open_prs
    if [ "$#" -eq 0 ]; then
        printf '%s has no open pull requests\n' "$repository"
    fi
fi

for pr in "$@"; do
    check_pull_request "$pr"
done

if [ "$dry_run" = 1 ]; then
    printf 'Dry run finished: %s review(s) would be hidden.\n' "$would_hide"
else
    printf 'Finished: %s review(s) hidden.\n' "$hidden_count"
fi

if [ "$failures" -gt 0 ]; then
    printf '%s problem(s) need attention; see the errors above.\n' "$failures" >&2
    exit 1
fi
