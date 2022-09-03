name: Bug Report
description: Report a bug in the .NET MAUI Toolkit
title: "[BUG] "
labels: [bug, unverified]
body:
- type: checkboxes
  attributes:
    label: Is there an existing issue for this?
    description: Please search to see if an issue already exists for the bug you encountered.
    options:
    - label: I have searched the existing issues
      required: true
- type: textarea
  attributes:
    label: Current Behavior
    description: A concise description of what you're experiencing.
  validations:
    required: true
- type: textarea
  attributes:
    label: Expected Behavior
    description: A concise description of what you expected to happen.
  validations:
    required: true
- type: textarea
  attributes:
    label: Steps To Reproduce
    description: Steps to reproduce the behavior.
    placeholder: |
      1. Open and run solution fron reproduction repository.
      1. Click the button and observe the bug 🐞
  validations:
    required: true
- type: input
  attributes:
    label: Link to public reproduction project repository
    description: Please add a link to a public [reproduction project](https://github.com/dotnet/maui/blob/main/.github/repro.md) repository. Otherwise the issue will be marked with "Needs reproduction" label and will have a lower priority. Attached zip files cannot be opened by us.
  validations:
    required: true
- type: textarea
  attributes:
    label: Environment
    description: |
      examples:
        - **.NET MAUI CommunityToolkit**: 1.2.0
        - **OS**: Windows 10 Build 10.0.19041.0
        - **.NET MAUI**: 6.0.408
    value: |
        - .NET MAUI CommunityToolkit:
        - OS:
        - .NET MAUI:
    render: markdown
  validations:
    required: true
- type: textarea
  attributes:
    label: Anything else?
    description: |
      Links? References? Anything that will give us more context about the issue you are encountering!
  validations:
    required: false
- type: markdown
  attributes:
    value: |
      Thanks for taking the time to fill out this bug report.