# MCP Git Server with Docker in Windsurf: Diagnosis and Fix

## The Problem

The Git MCP (provided by `devin/git`) was not working in Windsurf. Every git command returned a generic Cascade orchestrator error:

```
error executing cascade step: CORTEX_STEP_TYPE_MCP_TOOL
```

Affected commands were `git_status`, `git_diff`, `git_log`, etc.

## Step-by-Step Diagnosis

### Step 1: Verify the Docker Container Was Alive

The `mcp/git` container showed up as running with `docker ps`, but inspecting its logs revealed that **every MCP response returned `"isError": true`**.

```json
{"jsonrpc":"2.0","id":3,"result":{"content":[{"type":"text","text":"/home/<user>/projects/my-repo"}],"isError":true}}
```

### Step 2: Check Mounted Volumes

```bash
docker inspect <container_id> --format='{{range .Mounts}}{{.Type}}: {{.Source}} -> {{.Destination}}{{println}}{{end}}'
```

Result: **no volumes mounted** (`No output`).

The Git MCP Docker container had no access to the host filesystem. When it received a `repo_path` like `/home/<user>/projects/my-repo`, that directory simply **did not exist inside the container**.

### Step 3: Find the MCP Configuration

In Windsurf, the MCP server configuration lives at:

```
~/.codeium/windsurf/mcp_config.json
```

The original Git MCP block was:

```json
"devin/git": {
  "args": [
    "run",
    "-i",
    "--rm",
    "mcp/git"
  ],
  "command": "docker",
  "registry": "devin/git"
}
```

Critical observation: **the `-v` argument was missing** to mount the repo directory into the container.

### Step 4: First Fix Attempt (Partial)

A volume pointing to the user's home directory was added:

```json
"-v",
"/home/<user>:/home/<user>",
```

> **Replace `<user>` with your actual username (e.g. `/home/john:/home/john`).**

This allowed the container to see the files, but a **second problem** appeared:

### Step 5: Git Ownership Error

```
fatal: detected dubious ownership in repository at '/home/<user>/projects/my-repo'
```

By default, the Docker container runs as `root`, but the repo files belong to the host user (`UID 1000`). Git has a security protection that refuses to operate on repositories owned by a different user.

We tried adding the Docker flag `-u 1000:1000` to run the container as the host user, but the `mcp/git` image does not contain the user `1000`, causing the container to fail to start.

### Step 6: Final Solution

Instead of changing the container user, we configured Git to accept any directory as safe using **environment variables**:

```json
"-e",
"GIT_CONFIG_COUNT=1",
"-e",
"GIT_CONFIG_KEY_0=safe.directory",
"-e",
"GIT_CONFIG_VALUE_0=*",
```

This tells Git inside the container that all directories are `safe.directory`, bypassing the ownership error without modifying the execution user.

## Final Configuration

The complete Git MCP block in `~/.codeium/windsurf/mcp_config.json` looks like this:

```json
{
  "mcpServers": {
    "devin/git": {
      "args": [
        "run",
        "-i",
        "--rm",
        "-v",
        "/home/<user>:/home/<user>",
        "-e",
        "GIT_CONFIG_COUNT=1",
        "-e",
        "GIT_CONFIG_KEY_0=safe.directory",
        "-e",
        "GIT_CONFIG_VALUE_0=*",
        "mcp/git"
      ],
      "command": "docker",
      "registry": "devin/git"
    }
  }
}
```

> **Replace `<user>` with your actual username.**
> **Note:** The volume mounts the entire `/home/<user>` directory so the MCP works with **any repository** you open in Windsurf, not just a specific one.

## Steps to Apply the Fix

1. Open `~/.codeium/windsurf/mcp_config.json` and update the Git MCP block with the configuration above (remember to replace `<user>`).
2. The MCP server will restart automatically — no manual action needed. Test any git command from Windsurf (e.g. `git status`, `git diff`).

## Key Takeaway

When an MCP server runs inside Docker, **the container needs access to the host filesystem**. Without a mounted volume (`-v`), the MCP sees an isolated filesystem and cannot read or write project files. Additionally, tools running inside the container (like `git`) may hit ownership issues when the container and host use different users.
