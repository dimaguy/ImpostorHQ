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

window.onload = onload();
function onload() {
	var tokens = []
	plot();
	const randomarray = (length, max) =>
		Array(length).fill().map(() => Math.round(Math.random() * max));
	tokens = randomarray(5,100)
	document.getElementById("Lobbies").innerHTML = tokens[0];
	document.getElementById("Players").innerHTML = tokens[1];
	document.getElementById("Uptime").innerHTML = tokens[2];
	playersOnline = tokens[1];
	lobbies = tokens[0];
	cpuUsage = tokens[3];
	ramUsage = tokens[4];
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
