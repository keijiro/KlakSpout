# Workflow Instructions for Agents

## Project Structure

The Packages directory contains the primary UPM package developed in this
project. Its name follows the jp.keijiro.[package-name] pattern.

The repository root contains README.md, CHANGELOG.md, and LICENSE. Equivalent
files exist inside the package, so keep them synchronized with the root copies
whenever you update the root-level documents.

## Action Definition: Updating the Changelog

Updating the changelog means bringing the [Unreleased] section of CHANGELOG.md
up to date. Review git commits made since the previous release and append the
relevant changes to that section.

The [Unreleased] section may already include manually written text. Proofread it
and adjust the wording so that it matches the newly added entries. Add the
[Unreleased] section if it is missing.

## Action Definition: Preparing a Package Release

Preparing a package release means refreshing the UPM package data inside the
Packages directory so it is ready for a new version.

Perform the following tasks:

- Bump the version field in package.json.
- Update the `_upm` element in package.json as described below.
- Change the [Unreleased] heading in CHANGELOG.md to the new version and
  today's date.
- Commit the changes and create a git tag for the new version number.

## About the `_upm` Element in package.json

The `_upm` element in package.json contains only the `changelog` entry. Copy the
latest version section from CHANGELOG.md into that entry, but remove the section
heading (version number and a date) and convert the content to Unity Rich Text.
Use `<b>` tags for headings and `<br>` for line breaks. Insert an extra `<br>`
before every heading after the first one.

## Action Definition: Releasing the Package

Releasing the package means publishing the tarball exported from Unity as the
new version.

Perform the following tasks:

- Ensure a `[package-name]-[version].tgz` tarball exists in the repository root.
  This file must be exported manually from the Unity Editor, so ask the user to
  do so if it is missing.
- Push commits and tags to the remote repository before creating the release.
- Use the gh command to create a GitHub release. Copy the latest CHANGELOG.md
  section into the release notes.
- Make the release title a concise summary in the form `[version]: [title]`.
  Confirm the chosen title with the user before finalizing the release.
- Use npm to publish the tarball.
