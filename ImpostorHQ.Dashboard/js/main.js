function connect() {
	//this function is used to connect to the server, and log in.
	var serverUrl;
	var scheme = "ws";
	
	//we must check if the website is using SSL.
	if (document.location.protocol === "https:") {
		scheme += "s";
	}
	
	//the listenport value will be replaced by the loader.
	serverUrl = scheme + "://" + document.location.hostname + ":22023";

	connection = new WebSocket(serverUrl);
	console.log("[DBG] Created WebSocket.");
  
	connection.onopen = function(evt) {
		//now that we are connected, we must authenticate.
		console.log("[DBG] WebSocket connected.");
		document.getElementById("status").innerHTML = "";
		var auth = {
			Text: document.getElementById("apikey").value,
			Type: MessageFlags.LoginApiRequest,
			Date: Date.now()
		};
		connection.send(JSON.stringify(auth));
	};
	console.log("WebSocket OnOpen is now hooked.");

	//we got a weird error, from the network...
	connection.onerror = function(event) {
		console.error("WebSocket error observed: ", event);
		document.getElementById("status").innerHTML = "WebSocket Error: " + event.type;
		document.getElementById("text").value = "";
		document.getElementById("text").disabled = true;
		document.getElementById("send").disabled = true;
	};
	console.log("WebSocket OnError is now hooked.");

	//we have received a mesage from the server.
	//here, we handle everything related to server messages, including authentication.
	connection.onmessage = function(evt) {
		console.log("WebSocket message received.");
		var box = document.getElementById("chatbox");
		var final_text = "";
		var json_data = JSON.parse(evt.data);
		console.dir(json_data);
		var time = new Date(json_data.Date);
		var timeStr = time.toLocaleTimeString();

		switch(json_data.Type) {
			case MessageFlags.ConsoleLogMessage:
				final_text = "(" + timeStr + ") [" + json_data.Name + "] : " + json_data.Text + "\n";
			break;
			case MessageFlags.LoginApiAccepted: //	MessageType : LoginApiAccepted
				document.getElementById("status").innerHTML = "Logged in!";
				document.getElementById("status").style.color = "green";
				document.getElementById("text").value = "";
				document.getElementById("text").disabled = false;
				document.getElementById("send").disabled = false;
				console.log("Authenticated.")
			break;

			case MessageFlags.LoginApiRejected:
				document.getElementById("status").style.color = "red";
				document.getElementById("status").innerHTML = "Api Key Rejected!";
				document.getElementById("text").disabled = true;
				document.getElementById("send").disabled = true;
			break;

			case MessageFlags.DoKickOrDisconnect:
				document.getElementById("status").style.color = "red";
				document.getElementById("status").innerHTML = "Kicked:" + json_data.Text;
				document.getElementById("text").value = "";
				document.getElementById("text").disabled = true;
				document.getElementById("send").disabled = true;
			break;

			case MessageFlags.HeartbeatMessage:
				var tokens = json_data.Text.split("-");
				document.getElementById("hblabel").innerHTML = "Lobbies: " + tokens[0] + " Players: " + tokens[1];
				console.log("Heartbeat received.")
			
			break;

			//	commented out for now, but could be used to transmit game room list
			//      case "userlist":
			//        var ul = "";
			//        var i;
			//
			//        for (i=0; i < msg.users.length; i++) {
			//          ul += msg.users[i] + "<br>";
			//        }
			//        document.getElementById("userlistbox").innerHTML = ul;
			//        break;
		}

		if (final_text.length) {
			  box.value += final_text; 
			  box.scrollTop = box.scrollHeight; 
		}
	};
	console.log("WebSocket message callback active.");

}

function send() {
	//this will send a message over the socket, to our server.
	console.log("***SEND");
	var msg = {
		Text: document.getElementById("text").value,
		Type: MessageFlags.ConsoleCommand, //message
		Date: Date.now()
	};
	connection.send(JSON.stringify(msg));
	document.getElementById("text").value = "";
}

function handleKey(evt) {
	//this will handle keys that trigger events. [e.g : enter will send the command]
	if (evt.keyCode === 13 || evt.keyCode === 14) {
		if (!document.getElementById("send").disabled) {
		send();
		}
	}
}
const MessageFlags = 
{
	LoginApiRequest : "0",      // A request to log in, with a given API key.
    LoginApiAccepted : "1",     // The API Key is correct, so the login is successful.
    LoginApiRejected : "2",     // The API key is incorrect, so the login is rejected.
    ConsoleLogMessage : "3",    // The only working text message, so far.
    ConsoleCommand : "4",       // A command sent from the dashboard to the API.
    HeartbeatMessage : "5",     // Not implemented yet.
    GameListMessage : "6",      // Not implemented yet.
    DoKickOrDisconnect : "7"    // A message when a client is kicked (not implemented) or the server shuts down.
}
