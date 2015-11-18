
window.onload = function () {

      var socket = io();

       var heart =  document.getElementById("heart");
       
       socket.on('rate', function (msg) {
          
           var pulse = 60/msg/2
           document.getElementById("label").innerText = msg;
           
           heart.style.transition = "transform " + pulse + "s ease-in-out, opacity " + pulse + "s ease-in-out"
      });
       
      heart.addEventListener("transitionend", loopTransition, false);
      heart.className = "stateTwo";
      
      function loopTransition(e) {
      if (e.propertyName == "opacity") {
            if (heart.className == "stateTwo") {
                  heart.className = "stateOne";
            } else {
                  heart.className = "stateTwo";
            }
      }
      }

};

