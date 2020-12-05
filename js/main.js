"use strict";
var playersOnline = 0;
var lobbies = 0;
var cpuUsage = 0;
var ramUsage = 0;
var _playerchart = null;
var _cpuchart = null;
var _ramchart = null;

var playerChart = document.getElementById('playerChart');
var ctxPlayers = playerChart.getContext('2d');

var cpuChart = document.getElementById('cpuChart');
var ctxCpu = cpuChart.getContext('2d');

var ramChart = document.getElementById('ramChart');
var ctxRam = ramChart.getContext('2d');
var tokens = []
var uptime = 0
var delaytime = 0
var lobbynum = 0
const delay = ms => new Promise(res => setTimeout(res, ms));
window.onload = onload();
async function onload() {
	plot();
	setInterval(setTime, 1000);
	while (true) {
		const randomarray = (length, min, max) =>
			Array(length).fill().map(() => getRndInteger(min, max));
		tokens = randomarray(5, 1, 100);
		delaytime = getRndInteger(2000, 10000);
		lobbynum = getRndInteger(Math.round(tokens[1] / 10), tokens[1]);
		document.getElementById("Lobbies").innerHTML = lobbynum;
		document.getElementById("Players").innerHTML = tokens[1];
		playersOnline = tokens[1];
		lobbies = lobbynum;
		cpuUsage = tokens[3];
		ramUsage = Math.round(Math.random() * 1024);
		await delay(delaytime);
	}
}
function getRndInteger(min, max) {
	return Math.floor(Math.random() * (max - min + 1)) + min;
}
function setTime() {
	++uptime;
	document.getElementById("Uptime").innerHTML = pad(parseInt(uptime / 60));
}
function pad(val) {
	var valString = val + "";
	if (valString.length < 2) {
		return "0" + valString;
	} else {
		return valString;
	}
}
function plot() {
	_playerchart = new Chart(ctxPlayers, {
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
	_cpuchart = new Chart(ctxCpu, {
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
	_ramchart = new Chart(ctxRam, {
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
};
