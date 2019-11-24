"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/queueHub").build();

//get roomNo from URL
var pathElements = window.location.pathname.split('/');
var roomNo = pathElements[pathElements.length - 1];

connection.on("ReceiveQueueNo", function (user, message) {
    DistributeQueueMessage(message);
});

connection.on("ReceiveAdditionalInfo", function (id, message) {
    document.getElementById("additionalInfo").innerHTML = message;
    FooterVisibility(message)
});

connection.on("Refresh", function (roomNo) {
    console.log("refresh");
    location.reload();
});

connection.on("ReceiveDoctorFullName", function (user, message) {
    console.log(message);
    document.getElementById("DoctorFullName").textContent = message;
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
        connection.invoke("RegisterPatientView", roomNo).catch(function (err) {
            console.log("Register error");
            return console.error(err.toString());
        });
    }).catch(function (err) {
        console.log("Hub Start error");
        console.error(err.toString());
        setTimeout(reconnect(), 5000);
    });
}

function DistributeQueueMessage(message) {
    var mainField = document.getElementById("QueueNo");
    var secondField = document.getElementById("QueueMessageExtension");
    var headerField = document.getElementById("DoctorFullName");
    if (message.search("NZMR") === 0) {
        var firstPart = message.split(" ")[0];
        mainField.textContent = firstPart;
        secondField.textContent = message.substring(firstPart.length, message.length);
        headerField.hidden = true;
        mainField.style.paddingTop = "0.4em";
    }
    else {
        mainField.textContent = message;
        secondField.textContent = "";
        headerField.hidden = false;
        mainField.style.paddingTop = "0";
    }
}

function FooterVisibility(message) {
    if (message.length > 0) {
        $('footer').show();
    }
    else {
        $('footer').hide();
    }
}