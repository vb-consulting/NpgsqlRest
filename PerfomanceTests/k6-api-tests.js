import { check } from "k6";
import http from "k6/http";

const type = __ENV.TYPE;

var baseUrl;
if (type.toLowerCase() == "postgrest") {
    baseUrl = "http://127.0.0.1:3000/rpc/"
} else if (type.toLowerCase() == "npgsqlrest") {
    baseUrl = "http://127.0.0.1:5000/api/"
}
else {
    throw type;
}

const func = __ENV.FUNC;
if (["perf_test", "perf_test_arrays", "perf_test_record", "perf_test_record_arrays"].indexOf(func) == -1) {
    throw func;
}

const url = baseUrl + func;
const duration = __ENV.DURATION || "60s";
const target = Number(__ENV.TARGET || "100");
const records = Number(__ENV.RECORDS || "10");

// define configuration
export const options = {
    // define thresholds
    thresholds: {
        http_req_failed: [{ threshold: "rate<0.01", abortOnFail: true }], // availability threshold for error rate
        http_req_duration: ["p(99)<1000"], // Latency threshold for percentile
    },
    // define scenarios
    scenarios: {
        breaking: {
            executor: "ramping-vus",
            stages: [
                { duration: duration, target: target },
            ],
        },
    },
};

export default function () {
    const payload = JSON.stringify({
        _records: records,
        _text_param: "ABCXYZ",
        _int_param: 99,
        _ts_param: "2024-01-19",
        _bool_param: true
    });
    const params = {
        headers: {
            "Content-Type": "application/json",
        },
    };

    // send a post request and save response as a variable
    const res = http.post(url, payload, params);

    // check that response is 200
    check(res, {
        "response code was 200": (res) => res.status == 200,
    });
}
