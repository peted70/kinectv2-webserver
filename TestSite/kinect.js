var socket;
var myImage;
var context;
var feed;
var canvasWidth;
var canvasHeight;

document.addEventListener("DOMContentLoaded", function (event) {
    var canvas = document.getElementById("myCanvas");
    canvasWidth = canvas.width;
    canvasHeight = canvas.height;
    context = canvas.getContext("2d");
    context.fillStyle = "#31B131";

    feed = new Image(); 
    feed.onload = function () {
        context.drawImage(feed, 0, 0);
    };

    myImage = document.getElementById('myImg');
    console.log('about to open socket');
    socket = new WebSocket('ws://localhost:1337/kinect');
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
        //context.clearRect(0, 0, canvasWidth, canvasHeight);
        if (event.data instanceof Blob) {
            var colData = window.URL.createObjectURL(event.data);
            feed.src = colData;
            window.URL.revokeObjectURL(colData);
        } else {
            var bodies = JSON.parse(event.data);
            for (var i = 0; i < bodies.length; i++) {
                var jointDictionary = bodies[i];
                for (var joint in jointDictionary) {
                    var jnt = jointDictionary[joint];
                    var x = jnt.X;
                    var y = jnt.Y;
                    context.fillRect(x - 8, y - 8, 16, 16);
                }
            }
        }
    };
});