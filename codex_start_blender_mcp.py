import importlib.util
import os
import sys
import traceback

import bpy


LOG_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "codex-blender-mcp-heartbeat.log")


def log(message):
    with open(LOG_PATH, "a", encoding="utf-8") as handle:
        handle.write(message + "\n")
    print(message)


def load_blender_mcp_addon():
    addon_path = os.path.join(
        os.environ["LOCALAPPDATA"],
        "Programs",
        "Blender-MCP",
        "addon.py",
    )

    module_name = "codex_blender_mcp_addon"
    if module_name in sys.modules:
        return sys.modules[module_name]

    spec = importlib.util.spec_from_file_location(module_name, addon_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load Blender MCP addon from {addon_path}")

    module = importlib.util.module_from_spec(spec)
    sys.modules[module_name] = module
    spec.loader.exec_module(module)
    return module


try:
    log("Codex bootstrap: starting")
    addon = load_blender_mcp_addon()

    if not hasattr(bpy.types.Scene, "blendermcp_port"):
        addon.register()

    port = int(os.environ.get("BLENDERMCP_PORT", "9876"))
    bpy.context.scene.blendermcp_port = port

    server = getattr(bpy.types, "blendermcp_server", None)
    if server is None:
        server = addon.BlenderMCPServer(port=port)
        bpy.types.blendermcp_server = server

    if not server.running:
        server.start()

    bpy.context.scene.blendermcp_server_running = server.running
    log(f"Codex bootstrap: Blender MCP running={server.running} port={port}")

    def heartbeat():
        server = getattr(bpy.types, "blendermcp_server", None)
        running = bool(server and server.running)
        socket_open = bool(server and getattr(server, "socket", None))
        log(f"Codex heartbeat: running={running} socket={socket_open} background={bpy.app.background}")
        return 2.0

    bpy.app.timers.register(heartbeat, first_interval=2.0)
except Exception:
    log("Codex bootstrap: failed to start Blender MCP")
    traceback.print_exc()
