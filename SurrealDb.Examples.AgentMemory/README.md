# Agent Memory Playground

An interactive console example for the `SurrealDb.AgentMemory` client.

## Configuration

Set these environment variables. All three are required.

- `SURREAL_AGENT_ENDPOINT`: the Agent Memory endpoint origin, without `/api/v1/...`.
- `SURREAL_AGENT_CONTEXT_ID`: the context to use.
- `SURREAL_AGENT_API_KEY`: an API key for that context.

## Try it out

```text
/remember Ada works on a .NET SDK and likes short answers.
/recall What does Ada prefer?
/chat What is Ada working on?
/session
/quit
```

`/chat` streams the reply as it is written, and keeps the conversation in one session.
