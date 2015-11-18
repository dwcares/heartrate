var express = require('express');
var app = express();
app.use(express.static('public')); 
var http = require('http').Server(app);
var io = require('socket.io')(http);
var port = process.env.PORT || 3000;

app.get('/', function(req, res) {
 res.sendFile(__dirname + '/public/index.html');
});

io.on('connection', function(socket) {
    console.log('new connection ' + socket);
    
    socket.on('rate', function(msg) {
        console.log(msg);
        socket.broadcast.emit('rate', msg);
    });
    
    socket.on('disconnect', function(msg) {
       console.log(msg);
    });            
});

http.listen(port, function() {
    console.log('listening on *: ' + port);
});