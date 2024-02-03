# Performance Tests

This directory contains files required for performance tests with the [Grafana K6](https://k6.io/) REST API load and performance testing tool.

Used API's are from:

- Latest [NpgsqlRest AOT build](https://github.com/vb-consulting/NpgsqlRest/tree/master/AotBuildTemplate).
- [PostgREST 12.0.2](https://github.com/PostgREST/postgrest/releases/tag/v12.0.2)

## Files

- `appsettings.json` - configuration for NpgsqlRest used in testing.
- `k6-api-tests.js` - K6 testing script.
- `perf_tests_script.sql` - database script that creates functions that are tested.
- `perf_tests.http` - HTTP file for smoke tests for both systems.
- `postgrest.conf` - PostgREST configuration file used in testing.
- `readme.md` - this file.
- `test-script.sh` - shell script that orchestrates and runs all tests.
- `results` - directory with the raw dump of text files from the testing session.

## Results

| Records | Function   | NpgsqlRest Requests | PostgREST Requests | Ratio |
| ------: | ---------: | ---------: | --------: | --------: |
| 10 | `perf_test` | 392.212 | 74.621 | 5.26 |
| 100 | `perf_test` | 407.844 | 60.704 | 6.72 |
| 10 | `perf_test_arrays` | 292.392 | 50.549 | 5.78 |
| 100 | `perf_test_arrays` | 240.158 | 50.911 | 4.72 |
| 10 | `perf_test_record` | 518.515 | 57.665 | 8.99 |
| 100 | `perf_test_record` | 439.220 | 59.413 | 7.39 |
| 10 | `perf_test_record_arrays` | 383.549 | 52.951 | 7.24 |
| 100 | `perf_test_record_arrays` | 338.835 | 51.507 | 6.58 |