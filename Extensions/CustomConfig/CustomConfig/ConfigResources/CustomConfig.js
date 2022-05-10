var websocket = null;
document.addEventListener('DOMContentLoaded', () => {
    websocket = new WebSocket(window.hmiWsEndpoint());

    websocket.onmessage = event => {
        var response = JSON.parse(event.data);
        var command = response.commands[0];

        if (command.error && command.error.reason) {
            document.getElementById('output').value = JSON.stringify(command, null, 2);
        } else if (command.symbol == window.hmiCurrentDomain() + '.GetRandom') {
            document.getElementById('output').value = JSON.stringify(command, null, 2);
        }
    };
});

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('random-btn').addEventListener('click', () => {
        if (websocket == null) return;

        websocket.send(JSON.stringify(
            {
                "commands": [{
                    "commandOptions": [
                        "SendErrorMessage"
                    ],
                    "symbol": window.hmiCurrentDomain() + ".GetRandom"
                }]
            }
        ));
    });
});