## basic_sse_server

This is a simple server that uses Server-Sent Events (SSE) to send messages to connected clients. 
(There is no MCP stuff in this project, it is here to demonstrate how to use SSE in a simple server.)

This server has two endpoints:

- `/sse`: This endpoint is used to send messages to connected clients.
- `/hello/{message}`: This endpoint allows clients to send a message to the server, which will then broadcast the message to all connected clients.