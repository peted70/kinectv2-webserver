var socket;
var myImage;
var context;
var feed;

document.addEventListener("DOMContentLoaded", function (event) {
    context = document.getElementById("myCanvas").getContext("2d");

    feed = new Image(); 
    feed.onload = function () {
        context.drawImage(feed, 0, 0);
    };

    myImage = document.getElementById('myImg');
    console.log('about to open socket');
    socket = new WebSocket('ws://localhost:2012/kinect');
    console.log('attempted to open socket');

    socket.onopen = function () {
        console.log('socket opened');
    };
    socket.onclose = function () {
        console.log('socket closed');
    };
    socket.onerror = function (err) {
        console.log('error - ' + err);
    };
    socket.onmessage = function (event) {
        var colData = window.URL.createObjectURL(event.Data); 
        feed.src = colData;
        window.URL.revokeObjectURL(colData);
    };
});