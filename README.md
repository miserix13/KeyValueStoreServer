# KeyValueStoreServer

## A key-value store service implemementation that uses ZoneTree for storage

### ZoneTree: <https://github.com/koculu/ZoneTree>

## Features

### -- Key-value database

### -- LSM tree

### -- TCP for network communication

## TCP protocol

The server accepts line-delimited UTF-8 commands and responds with a single line per command.

- `PING` -> `PONG`
- `GET <guid>` -> `VALUE <base64>` or `NOT_FOUND`
- `SET <guid> <base64>` -> `OK`
- `DELETE <guid>` -> `OK`
- `QUIT` -> `BYE`

Errors return `ERROR <message>`.
