"use strict";
var connection = null;
var y = null;
var x = null;
var playersOnline = 0;
var lobbies = 0;
/*
var chart = new SmoothieChart({
	tooltip: true,
	timestampFormatter: SmoothieChart.timeFormatter,
	maxValue: 100,
	minValue: 0
});
var canvas = document.getElementById('cpu-chart');
var cpu = new TimeSeries();
*/
var playerChart = document.getElementById('playerChart');
var ctx = playerChart.getContext('2d');

//chart.addTimeSeries(playerChart, { lineWidth: 2, strokeStyle: '#00ff00' });

function connect() {
	var serverUrl;
	var scheme = "ws";

	if (document.location.protocol === "https:") {
		scheme += "s";
	}
	
	serverUrl = scheme + "://" + document.location.hostname + ":22023";

	connection = new WebSocket(serverUrl);
	console.log("***CREATED WEBSOCKET");
  
	connection.onopen = function(evt) {
		console.log("***ONOPEN");
		document.getElementById("status").innerHTML = "";
		var auth = {
			Text: document.getElementById("apikey").value,
			Type: MessageFlags.LoginApiRequest,
			Date: Date.now()
		};
		connection.send(JSON.stringify(auth));
	};
	console.log("***CREATED ONOPEN");

	connection.onerror = function(event) {
		console.error("WebSocket error observed: ", event);
		document.getElementById("status").innerHTML = "WebSocket Error: " + event.type;
		document.getElementById("text").value = "";
		document.getElementById("text").disabled = true;
		document.getElementById("send").disabled = true;
	};
	console.log("***CREATED ONERROR");

	connection.onmessage = function(evt) {
		console.log("***ONMESSAGE");
		var box = document.getElementById("chatbox");
		var text = "";
		var msg = JSON.parse(evt.data);
		console.log("Message received: ");
		console.dir(msg);
		var time = new Date(msg.Date);
		var timeStr = time.toLocaleTimeString();

		switch (msg.Type)
        {
            case MessageFlags.ConsoleLogMessage:
				text = "(" + timeStr + ") [" + msg.Name + "] : " + msg.Text + "\n";
				break;
			case MessageFlags.LoginApiAccepted:
				document.getElementById("status").style.color = "green";
				document.getElementById("status").innerHTML = "Logged in!";
				document.getElementById("text").value = "";
				document.getElementById("text").disabled = false;
				document.getElementById("send").disabled = false;
				//chart.addTimeSeries(series, { lineWidth: 2, strokeStyle: '#00ff00' });
                plot();
				console.log("AUTHED");
				break;

			case MessageFlags.LoginApiRejected:
				document.getElementById("status").style.color = "red";
				document.getElementById("status").innerHTML = "Api Key Error!";
				document.getElementById("text").disabled = true;
				document.getElementById("send").disabled = true;
				break;

			case MessageFlags.DoKickOrDisconnect:
				document.getElementById("status").style.color = "red";
				document.getElementById("status").innerHTML = "Kicked:" + msg.Text;
				document.getElementById("text").value = "";
				document.getElementById("text").disabled = true;
				document.getElementById("send").disabled = true;
				break;

			case MessageFlags.HeartbeatMessage:
				var tokens = msg.Flags;
				document.getElementById("Lobbies").innerHTML = tokens[0];
				document.getElementById("Players").innerHTML = tokens[1];
				document.getElementById("Uptime").innerHTML = tokens[2];
				playersOnline = tokens[1];
				lobbies = tokens[0];
				//series.append(new Date().getTime(), tokens[3]);
				//chart.streamTo(canvas, 5000);
				console.log("HEARTBEAT")
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

		if (text.length) {
			  box.value += text; 
			  box.scrollTop = box.scrollHeight; 
		}
	};
	console.log("***CREATED ONMESSAGE");

}

function send() {
	console.log("***SEND");
	var msg = {
		Text: document.getElementById("text").value,
		Type: MessageFlags.ConsoleCommand,
		Date: Date.now()
	};
	connection.send(JSON.stringify(msg));
	document.getElementById("text").value = "";
}
function plot() {
	var chart = new Chart(ctx, {
        type: 'line',
		data: {

			datasets: [{
				label: 'Players',
				borderColor: 'rgb(255, 99, 132)',
				backgroundColor: 'rgba(255, 99, 132, 0.5)',
				lineTension: 0,
				borderDash: [8, 4]

			},
			{
				label: 'Lobbies',
				borderColor: 'rgb(54, 162, 235)',
				backgroundColor: 'rgba(54, 162, 235, 0.5)',
				lineTension: 0,
			}

			]
		},

		options: {
			responsive: true,
			maintainAspectRatio: false,
			scales: {
				xAxes: [{
					type: 'realtime',
					realtime: {
						delay: 5000,
						duration: 60000 * 5,
						onRefresh: function (chart) {
							chart.data.datasets[0].data.push({
								x: Date.now(),
								y: playersOnline,

							});
							chart.data.datasets[1].data.push({
								x: Date.now(),
								y: lobbies,

							});
						}
					}
				}]
			}
		},

	});
}
function handleKey(evt) {
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
    ConsoleLogMessage : "3",    // Server Message
    ConsoleCommand : "4",       // A command sent from the dashboard to the API.
    HeartbeatMessage : "5",     // Quick sanity check with some statistics
    GameListMessage : "6",      // Not implemented yet.
    DoKickOrDisconnect : "7"    // A message when a client is kicked or the server shuts down.
}
