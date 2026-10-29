"""Run the opt-in development player scene test against a passive local transport.

This checks scene lifecycle without logging in, loading accounts, or sending
simulated match results. It is NOT a server/gameplay integration test.
The fixture refuses to bind when the real game server already uses port 9000.
"""
import argparse
from pathlib import Path
import socket
import subprocess
import threading


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--client', type=Path, required=True)
    parser.add_argument('--log', type=Path, required=True)
    args = parser.parse_args()
    args.log.parent.mkdir(parents=True, exist_ok=True)
    stop = threading.Event()
    listener = socket.socket()
    listener.bind(('127.0.0.1', 9000))
    listener.listen(4)
    listener.settimeout(.25)

    def consume(connection):
        with connection:
            connection.settimeout(.25)
            while not stop.is_set():
                try:
                    if not connection.recv(65536): return
                except socket.timeout: continue
                except OSError: return

    def accept():
        while not stop.is_set():
            try: connection, _ = listener.accept()
            except socket.timeout: continue
            except OSError: return
            threading.Thread(target=consume, args=(connection,), daemon=True).start()

    worker = threading.Thread(target=accept, daemon=True)
    worker.start()
    try:
        result = subprocess.run([str(args.client.resolve()), '-batchmode', '-nographics',
            '-verify-scene-transitions', '-logFile', str(args.log.resolve())], timeout=120,
            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        log = args.log.read_text(encoding='utf-8', errors='replace')
        if result.returncode != 0 or 'SCENE_TRANSITION_SMOKE_PASS' not in log:
            raise SystemExit('Scene smoke failed. Inspect ' + str(args.log))
        print('SCENE_SMOKE_PASS: nine transitions across six build scenes; no account or simulated game data.')
    finally:
        stop.set()
        listener.close()
        worker.join(1)


if __name__ == '__main__':
    main()
