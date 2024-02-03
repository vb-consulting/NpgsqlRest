#!/bin/bash
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test" --env RECORDS="10" > ./npgsqlrest-perf_test-10rec.txt
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test_arrays" --env RECORDS="10" > ./npgsqlrest-perf_test_arrays-10rec.txt
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test_record" --env RECORDS="10" > ./npgsqlrest-perf_test_record-10rec.txt
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test_record_arrays" --env RECORDS="10" > ./npgsqlrest-perf_test_record_arrays-10rec.txt

k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test" --env RECORDS="10" > ./npgsqlrest-perf_test-100rec.txt
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test_arrays" --env RECORDS="10" > ./npgsqlrest-perf_test_arrays-100rec.txt
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test_record" --env RECORDS="10" > ./npgsqlrest-perf_test_record-100rec.txt
k6 run api-test.js --env TYPE="npgsqlrest" --env FUNC="perf_test_record_arrays" --env RECORDS="10" > ./npgsqlrest-perf_test_record_arrays-100rec.txt


k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test" --env RECORDS="10" > ./postgrest-perf_test-10rec.txt
k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test_arrays" --env RECORDS="10" > ./postgrest-perf_test_arrays-10rec.txt
k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test_record" --env RECORDS="10" > ./postgrest-perf_test_record-10rec.txt
k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test_record_arrays" --env RECORDS="10" > ./postgrest-perf_test_record_arrays-10rec.txt

k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test" --env RECORDS="10" > ./postgrest-perf_test-100rec.txt
k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test_arrays" --env RECORDS="10" > ./postgrest-perf_test_arrays-100rec.txt
k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test_record" --env RECORDS="10" > ./postgrest-perf_test_record-100rec.txt
k6 run api-test.js --env TYPE="postgrest" --env FUNC="perf_test_record_arrays" --env RECORDS="10" > ./postgrest-perf_test_record_arrays-100rec.txt

