"use strict";
var connection = null;
var firstLogin = true
var playersOnline = 0;
var lobbies = 0;
var cpuUsage = 0;
var ramUsage = 0;

var playerChart = document.getElementById('playerChart');
var ctxPlayers = playerChart.getContext('2d');

var cpuChart = document.getElementById('cpuChart');
var ctxCpu = cpuChart.getContext('2d');

var ramChart = document.getElementById('ramChart');
var ctxRam = ramChart.getContext('2d');

const MessageFlags =
{
	LoginApiRequest: "0",      // A request to log in, with a given API key.
	LoginApiAccepted: "1",     // The API Key is correct, so the login is successful.
	LoginApiRejected: "2",     // The API key is incorrect, so the login is rejected.
	ConsoleLogMessage: "3",    // Server Message
	ConsoleCommand: "4",       // A command sent from the dashboard to the API.
	HeartbeatMessage: "5",     // Quick sanity check with some statistics
	GameListMessage: "6",      // Not implemented yet.
	DoKickOrDisconnect: "7"    // A message when a client is kicked or the server shuts down.
}

window.onload = onload();
function onload() {
	var autoapi = window.location.href.match(/\?apikey=(.*)/);
	console.log(autoapi);
	if (autoapi != null) {
	document.getElementById("apikey").value = autoapi[1];
	}
}

function connect() {
	var serverUrl;
	var scheme = "ws";

	if (document.location.protocol === "https:") {
		scheme += "s";
	}

	serverUrl = scheme + "://" + document.location.hostname + ":22023";

	connection = new WebSocket(serverUrl);
	console.log("***CREATED WEBSOCKET");

	connection.onopen = function (evt) {
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

	connection.onerror = function (event) {
		console.error("WebSocket error observed: ", event);
		document.getElementById("status").innerHTML = "WebSocket Error: " + event.type;
		document.getElementById("text").value = "";
		document.getElementById("text").disabled = true;
		document.getElementById("send").disabled = true;
	};
	console.log("***CREATED ONERROR");

	connection.onmessage = function (evt) {
		console.log("***ONMESSAGE");
		var box = document.getElementById("chatbox");
		var text = "";
		var msg = JSON.parse(evt.data);
		console.log("Message received: ");
		console.dir(msg);
		var time = new Date(msg.Date);
		var timeStr = time.toLocaleTimeString();

		switch (msg.Type) {
			case MessageFlags.ConsoleLogMessage:
				text = "(" + timeStr + ") [" + msg.Name + "] : " + msg.Text + "\n";
				break;
			case MessageFlags.LoginApiAccepted:
				document.getElementById("status").style.color = "green";
				document.getElementById("status").innerHTML = "Logged in!";
				document.getElementById("text").value = "";
				document.getElementById("PlayersCSV").href = "/players.csv?" + document.getElementById("apikey").value;
				document.getElementById("text").disabled = false;
				document.getElementById("send").disabled = false;
				if (!firstLogin) {
					_playerchart.destroy();
					_cpuchart.destroy();
					_ramchart.destroy();
				};
				plot();
				firstLogin = false
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
				document.getElementById("PlayersCSV").href = "";
				_playerchart.destroy();
				_cpuchart.destroy();
				_ramchart.destroy();
				break;

			case MessageFlags.HeartbeatMessage:
				var tokens = msg.Flags;
				document.getElementById("Lobbies").innerHTML = tokens[0];
				document.getElementById("Players").innerHTML = tokens[1];
				document.getElementById("Uptime").innerHTML = tokens[2];
				playersOnline = tokens[1];
				lobbies = tokens[0];
				cpuUsage = tokens[3];
				ramUsage = tokens[4];
				console.log("HEARTBEAT");
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
	if (document.getElementById("text").value != null) {
		console.log("***SEND");
		var msg = {
			Text: document.getElementById("text").value,
			Type: MessageFlags.ConsoleCommand,
			Date: Date.now()
		};
		connection.send(JSON.stringify(msg));
		document.getElementById("text").value = "";
	}
}
function plot() {
	var _playerchart = new Chart(ctxPlayers, {
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
				yAxes: [{
					ticks: {
						beginAtZero: true,
						precision: 0
					}
				}],
				xAxes: [{
					type: 'realtime',
					realtime: {
						delay: 5000,
						duration: 60000 * 5,
						onRefresh: function (chart) {
							chart.data.datasets[0].data.push({
								x: Date.now(),
								y: playersOnline
							});
							chart.data.datasets[1].data.push({
								x: Date.now(),
								y: lobbies
							});
						}
					}
				}]
			}
		},

	});
	var _cpuchart = new Chart(ctxCpu, {
		type: 'line',
		data: {

			datasets: [{
				label: 'CPU Usage (%)',
				borderColor: 'rgb(255, 0, 132)',
				backgroundColor: 'rgba(255, 99, 132, 0.5)',
				lineTension: 0,
				borderDash: [8, 4]

			}

			]
		},

		options: {
			responsive: true,
			maintainAspectRatio: false,
			scales: {
				yAxes: [{
					ticks: {
						beginAtZero: true,
						precision: 0
					}
				}],
				xAxes: [{
					type: 'realtime',
					realtime: {
						delay: 5000,
						duration: 60000,
						onRefresh: function (chart) {
							chart.data.datasets[0].data.push({
								x: Date.now(),
								y: cpuUsage
							});
						}
					}
				}]
			}
		},

	});
	var _ramchart = new Chart(ctxRam, {
		type: 'line',
		data: {

			datasets: [{
				label: 'Memory Usage (MB)',
				borderColor: 'rgb(255, 0, 255)',
				backgroundColor: 'rgba(255, 99, 132, 0.5)',
				lineTension: 0,
				borderDash: [8, 4]

			}

			]
		},

		options: {
			responsive: true,
			maintainAspectRatio: false,
			scales: {
				yAxes: [{
					ticks: {
						beginAtZero: true,
						precision: 0
					}
				}],
				xAxes: [{
					type: 'realtime',
					realtime: {
						delay: 5000,
						duration: 60000,
						onRefresh: function (chart) {
							chart.data.datasets[0].data.push({
								x: Date.now(),
								y: ramUsage
							});
						}
					}
				}]
			}
		},

	});
}

function handleSend(evt) {
	if (evt.keyCode === 13 || evt.keyCode === 14) {
		if (!document.getElementById("send").disabled) {
			send();
		}
	}
}
function HandleLogin(evt) {
	if (evt.keyCode === 13 || evt.keyCode === 14) {
		connect();
	}
}
