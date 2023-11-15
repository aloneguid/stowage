## 1.3.0

### Bugs fixed

- S3 buffer size needs to be at least 5Mb when performing multipart upload by @alexhughes05 in #14.
- S3 authentication would fail if file name contains spaces by @alexhughes05 in #14.

### Improvements

- Targeting `.NET 8`.

### Internal changes

CI/CD pipeline split into "build" and "release".
