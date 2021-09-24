---
name: Open a New Feature Proposal
about: For proposals that have been discussed in the Discussions tab and have been
  approved by a member of the core .NET MAUI Toolkit team
title: "[Proposal] "
labels: new, proposal
assignees: ''

---

<!--
Hello, and thanks for your interest in contributing to the .NET MAUI Toolkit! 

If you haven't been invited by a team member to open an Proposal, please instead open a Discussion at https://github.com/communitytoolkit/maui/discussions/new where we can discuss the pros/cons of the feature and its implementation. 

New language feature proposals should fully fill out this template. This should include a complete detailed design, which describes the syntax of the feature, what that syntax means, and how it affects current parts of the spec. Please make sure to point out specific spec sections that need to be updated for this feature.
-->
# [FEATURE_NAME]

* [x] Proposed
* [ ] Prototype: Not Started
* [ ] Implementation: Not Started
  * [ ] iOS Support
  * [ ] Android Support
  * [ ] macOS Support
  * [ ] Windows Support
* [ ] Documentation: Not Started
* [ ] Sample: Not Started

## Link to Discussion

<!-- Please link to the completed/approved [Discussion](https://github.com/communitytoolkit/maui/discussions)-->

## Summary
[summary]: #summary

<!-- One paragraph explanation of the feature. -->

## Motivation
[motivation]: #motivation

<!-- Why are we doing this? Which use cases does it enable? What is the expected outcome? -->

## Detailed Design
[design]: #detailed-design

<!-- This is the bulk of the proposal. Explain the design in enough detail for somebody familiar with .NET MAUI to understand, and for somebody familiar with the Community Toolkit to implement, and include examples of how the feature is used. Please include syntax and desired semantics for the change, including linking to the relevant parts of the existing .NET MAUI Toolkit spec to describe the changes necessary to implement this feature. An initial proposal does not need to cover all cases, but it should have enough detail to enable a community member to bring this proposal to design if they so choose. -->

## Usage Syntax
[usage]: #usage-syntax

<!-- Please provide an example of how an end-user will use this feature. If this is a UI control, please an example of the feature being consumed in both a C# UIand a XAML UI. -->

<!-- Here is an example from CharactersValidationBehavior:

### XAML Usage

```xml
<Entry>
    <Entry.Behaviors>
        <toolkit:CharactersValidationBehavior
            CharacterType="Digit"
            MaximumCharacterCount="10"
        />
    </Entry.Behaviors>
</Entry>
```

### C# Usage

```cs
var phoneEntry = new Entry();
phoneEntry.Behaviors.Add(new CharactersValidationBehavior
{
    CharacterType = CharacterType.Digit,
    MaximumCharacterCount = 10
});
```

-->

## Drawbacks
[drawbacks]: #drawbacks

<!-- Why should we *not* do this? -->

## Alternatives
[alternatives]: #alternatives

<!-- What other designs have been considered? What is the impact of not doing this? -->

## Unresolved Questions
[unresolved]: #unresolved-questions

<!-- What parts of the design are still undecided? -->
