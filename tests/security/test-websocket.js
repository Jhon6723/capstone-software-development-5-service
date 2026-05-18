import { check } from "k6";
import ws from "k6/ws";

const TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkMzhhZDQ0NC1iM2QxLTQ1OGMtYmMwOC1mOTRiYzdjODRiNzgiLCJlbWFpbCI6InRlc3QxQGV4YW1wbGUuY29tIiwianRpIjoiMGE5OTdkM2MtMGYzMi00ZWUzLTk3MWItMDE1YjEzYzk5ZjFjIiwiaWF0IjoxNzc4OTkxNTY4LCJleHAiOjE3Nzg5OTUxNjgsImlzcyI6Imh0dHBzOi8vYXBpLnBpeHByby5jb20iLCJhdWQiOiJodHRwczovL2FwaS5waXhwcm8uY29tIn0.Hcz1ayXBuANJL4Ca9_S-fFBpGHpzDBgr1h29iiQInh0"
export const options = {
  insecureSkipTLSVerify: true,
  scenarios: {
    dos_simulation: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "15s", target: 50 },  // ramp up to 50 VUs
        { duration: "30s", target: 50 },  // hold 50 VUs
        { duration: "10s", target: 0 },   // ramp down
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    ws_sessions: ["count>10"],
  },
};

export default function () {
  const url =
    `wss://localhost:8443/api/websocket/connect?access_token=${TOKEN}`;
  const res = ws.connect(
    url,
    {},
    function (socket) {
      socket.on("open", function () {
        console.log("Connected");
        socket.send(JSON.stringify({ type: "ping" }));

        // Keep connection alive and spam messages to simulate DoS
        socket.setInterval(function () {
          socket.send(JSON.stringify({ type: "ping" }));
        }, 1000);

        // Close after 45s per VU iteration
        socket.setTimeout(function () {
          socket.close();
        }, 45000);
      });

      socket.on("message", function (data) {
        console.log("Message received: " + data);
      });

      socket.on("close", function () {
        console.log("Disconnected");
      });

      socket.on("error", function (e) {
        console.error("WebSocket error: " + e.error());
      });
    },
  );
  console.log("Response: ", JSON.stringify(res));
  check(res, { "status is 101": (r) => r && r.status === 101 });
}
