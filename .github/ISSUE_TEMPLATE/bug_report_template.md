name: Bug Report
description: Report a bug in the .NET MAUI Toolkit
title: "[BUG] <title>"
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
    label: Steps To Reproduce (Attachment is required)
    description: Steps to reproduce the behavior.
    placeholder: |
      Please attach a small repro. Otherwise the issue will be marked with "Needs reproduction" label and will have a lower priority.
  validations:
    required: true
- type: dropdown
  attributes:
    label: CommunityToolkit Version
    description: What version of our library are you running?
    multiple: false
    options:
      - 1.0.0
      - 1.1.0
      - 1.2.0
  validations:
    required: true
- type: textarea
  attributes:
    label: Environment
    description: |
      examples:
        - **OS**: Windows 10 Build 10.0.19041.0
        - **.NET MAUI**: 6.0.408
    value: |
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