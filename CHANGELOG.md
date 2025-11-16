# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.6] - 2025-11-16

### Fixed

- Replaced the obsolete ShaderUtil API usage to remain compatible with Unity 6.3.

### Changed

- Changed the package release workflow from GitHub Action to a semi-manual
  approach with help of coding agents.

## [2.0.5] - 2025-11-14

### Fixed

- Game View capture mode didn't work correctly in the built-in render pipeline
  and HDRP.

## [2.0.4] - 2025-11-11

### Fixed

- Texture format mismatches in sender and receiver pipelines.

### Added

- Float texture sharing support with additional compatible formats.

### Changed

- Reworked the test/sample project for Unity 6 + URP.
- README rewritten for clarity and to document available pixel formats.
