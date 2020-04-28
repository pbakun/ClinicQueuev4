"use strict";

//roomNo, queueNo and id defined in Index.cshtml. Value taken from the model

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .configureLogging(signalR.LogLevel.Trace)
    .build();

//connection.serverTimeoutInMilliseconds = 15000;
//Set new QueueNoMessage if new one arrives. 
connection.on("ReceiveQueueNo", function (user, message) {
    document.getElementById("QueueNo").innerHTML = message;
});

connection.on("ReceiveAdditionalInfo", function (id, message) {
    document.getElementById("additionalInfo").value = message;
    var elementClassList = document.getElementById("SendAdditionalMessage").classList;
    if (message.length > 0) {
        elementClassList.replace("btn-dark", "btn-success");
    }
    else {
        elementClassList.replace("btn-success", "btn-dark");
    }
});

connection.on("NotifyQueueOccupied", function (message) {
    document.getElementById("serverMessage").innerHTML = message;
});

connectionStart();

connection.onclose(function () {
    console.log("Hub Connection Closed");
    reconnect();
});

function reconnect() {
    try {
        let started = connectionStart();
        console.log("Client restarted");
        return started;
    } catch (e) {
        console.error("Error reconnect");
        console.error(e.toString());
    }
}

function connectionStart() {
    connection.start().then(function () {
        connection.invoke("RegisterDoctor", roomNo).catch(function (err) {
            return console.error(err.toString());
        });
    }).catch(function (err) {
        console.log("Hub Start error");
        console.error(err.toString());
        setTimeout(reconnect(), 5000);
    });
}

document.getElementById("PrevNo").addEventListener("click", function (event) {
    if (queueNo > 0) {
        queueNo--;
        connection.invoke("QueueNoDown", roomNo).catch(function (err) {
            return console.error(err.toString());
        }); 
    }
    event.preventDefault();
});

document.getElementById("NextNo").addEventListener("click", function (event) {
    queueNo++;
    connection.invoke("QueueNoUp", roomNo).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("NewQueueNoSubmit").addEventListener("click", function (event) {
    var newNo = document.getElementById("NewQueueNoInputBox").value;
    ForceNewQueueNo(newNo);
    document.getElementById("NewQueueNoInputBox").value = "";
    event.preventDefault();
});

function ForceNewQueueNo(newNo) {
    newNo = parseInt(newNo);
    if (newNo > 0) {
        queueNo = newNo;
        connection.invoke("NewQueueNo", newNo, roomNo).catch(function (err) {
            return console.error(err.toString());
        });
    }
}

//send -1 (sets break to true) to the server
document.getElementById("Break").addEventListener("click", function (event) {
    connection.invoke("NewQueueNo", -1, roomNo).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("Special").addEventListener("click", function (event) {
    connection.invoke("NewQueueNo", -2, roomNo).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("SendAdditionalMessage").addEventListener("click", function (event) {
    var message = document.getElementById("additionalInfo").value;
    connection.invoke("NewAdditionalInfo", roomNo, message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

document.getElementById("ClearAdditionalMessage").addEventListener("click", function (event) {
    connection.invoke("NewAdditionalInfo", roomNo, '').catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});


