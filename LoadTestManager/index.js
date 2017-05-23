var Registry = require('azure-iothub').Registry.fromConnectionString("HostName=qisun-test-arm.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=xYOoyLI0wuMyOVTMDgYXpv4Jh+ww2b2NczVKQR7vlvU=");
var exec = require('child_process').exec;
var util = require('util');
var fs = require('fs');
var cmd = 'docker run -i --rm -v C:/Users/v-zhq/.m2:/root/.m2 -v C:/Users/v-zhq/IoT/javatest4/test2:/usr/src/mj -w /usr/src/mj maven:3.5.0-jdk-8 mvn exec:java -Dexec.args="%s %d"'; // remove compile cmd to avoid conflict volume


var cmd2 = 'mvn exec:java -Dexec.args="%s %d"';

var path = require("path");
var exeLocation = path.join(__dirname, '../', 'LoadTest\\bin\\Debug\\LoadTest.exe');

var cmd2 = exeLocation + ' "%s" "%d"';
var NUM_OF_DEVICES = 20;
var NUM_MESSAGE_PER_DEVICE = 10;
var stat = {
    numExec: 0,
    numDone: 0,
    numFail: 0,
    changedTimes: {},
    msgSent: {},
    msgRespond: {},
    devices: {}, // changed_times,msg_sent,msg_respond
};

const readline = require('readline');
const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
});

main();

var processList = [];

function main() {
    var deviceCS = [];
    Registry.list((err, deviceList) => {
        if (err) {
            console.log('mjerror')
        } else {
            console.log(`${deviceList.length} device(s) found.`);
            var reg = new RegExp("device(\\d+)");
            deviceList.forEach((device) => {
                var matches = reg.exec(device.deviceId);
                if (matches) {
                    if (parseInt(matches[1]) >= 0 && parseInt(matches[1]) < NUM_OF_DEVICES) {
                        deviceCS.push({
                            id: device.deviceId,
                            cs: "HostName=qisun-test-arm.azure-devices.net;DeviceId=" + device.deviceId + ";SharedAccessKey=" + device.authentication.symmetricKey.primaryKey
                        });
                    }
                }
            });
            console.log(util.format("Devices retrieved. (%d/%d)", deviceCS.length, NUM_OF_DEVICES));
            var beforeSendTimeStamp = Date.now();
            for (var i = 0; i < NUM_OF_DEVICES; i++) {
                var p = exec(util.format(cmd2, deviceCS[i].cs, NUM_MESSAGE_PER_DEVICE), { maxBuffer: 1024 * 500 }, (function(id, error, stdout, stderr) {

                    if (error) {
                        console.log("Error with " + id + "\n" + error);
                        stat.numDone++;
                        stat.numFail++;
                    } else {
                        console.log(util.format("Device %s complete. (%d/%d)", id, ++stat.numDone, NUM_OF_DEVICES));
                        stdout = stdout.substring(stdout.indexOf('Starting...'));
                    }

                    var m1 = stdout.match(/Device Twin changes sampling rate/g);
                    var m2 = stdout.match(/Start to send D2C message/g);
                    var m3 = stdout.match(/Sending D2C message success/g);

                    var totalConsumeTimeRegexp = /TotalTimeConsume: (\d*.\d*)/g;
                    var m4 = totalConsumeTimeRegexp.exec(stdout);

                    var getTwinTimeConsumeRegexp = /GetTwinTimeConsume: (\d*.\d*)/g;
                    var m5 = getTwinTimeConsumeRegexp.exec(stdout);

                    if (!stat.devices[id]) {
                        stat.devices[id] = {};
                    }
                    stat.devices[id].changed_times = (m1 == null ? 0 : m1.length);
                    stat.devices[id].msg_sent = (m2 == null ? 0 : m2.length);
                    stat.devices[id].msg_respond = (m3 == null ? 0 : m3.length);

                    stat.devices[id].totalConsumeTime = (m4 == null ? 0 : m4[1]);
                    stat.devices[id].getTwinCosumeTime = (m5 == null ? 0 : m5[1]);

                    fs.writeFile("log/" + id + ".log", stdout, (err) => {
                        if (err) {
                            console.log(err);
                        }
                    });
                    if (stat.numDone == NUM_OF_DEVICES) {
                        //finished

                        var afterSendTimeStamp = Date.now();

                        var totalGetTwinConsumTime = 0;
                        var totalConsumeTime = 0;
                        for (var d in stat.devices) {
                            if (!stat.changedTimes[stat.devices[d].changed_times]) {
                                stat.changedTimes[stat.devices[d].changed_times] = 1;
                            } else {
                                stat.changedTimes[stat.devices[d].changed_times]++;
                            }
                            if (!stat.msgSent[stat.devices[d].msg_sent]) {
                                stat.msgSent[stat.devices[d].msg_sent] = 1;
                            } else {
                                stat.msgSent[stat.devices[d].msg_sent]++;
                            }
                            if (!stat.msgRespond[stat.devices[d].msg_respond]) {
                                stat.msgRespond[stat.devices[d].msg_respond] = 1;
                            } else {
                                stat.msgRespond[stat.devices[d].msg_respond]++;
                            }

                            totalGetTwinConsumTime += parseFloat(stat.devices[d].getTwinCosumeTime);
                            totalConsumeTime += parseFloat(stat.devices[d].totalConsumeTime);
                        }
                        stat.avglGetTwinConsumTime = totalGetTwinConsumTime / NUM_OF_DEVICES;
                        stat.avgConsumeTime = totalConsumeTime / NUM_OF_DEVICES;
                        stat.totalConsumeTime = afterSendTimeStamp - beforeSendTimeStamp;
                        console.log(stat);
                        fs.writeFile("log/main.log", util.inspect(stat), () => {});
                    }
                }).bind(this, deviceCS[i].id));
                console.log(util.format("Device %s started. (%d/%d)", deviceCS[i].id, ++stat.numExec, NUM_OF_DEVICES));
                processList.push(p);
            }
        }
    });
}

rl.question("Press <ENTER> to open all devices\n", (answer) => {
    for (var p in processList) {
        processList[p].stdin.write("any\n");
    }
    // rl.close();
    rl.question("Press <ENTER> to stop all devices\n", (answer) => {
        for (var p in processList) {
            try {
                processList[p].stdin.write('any');
                processList[p].stdin.end();
            } catch (e) {
                console.log("Socket error : " + e.message);
            }
        }
        rl.close();
    })
})

function addDevice(num) {
    var devices = [];
    for (var i = 0; i < num; i++) {
        devices.push({ deviceId: "device" + i });
        if (devices.length == 100) {
            Registry.addDevices(devices, ((i, err, b, c) => {
                if (err) {
                    console.log("error" + err.message);
                }
                console.log("finished with " + i);
            }).bind(this, i + 1));
            devices = [];
        }
    }
    if (devices.length != 0) {
        Registry.addDevices(devices, ((i, err, b, c) => {
            if (err) {
                console.log("error" + err.message);
            }
            console.log("finished with " + i);
        }).bind(this, i + 1));
        devices = [];
    }
}

function removeDevice(num) {
    var devices = [];
    for (var i = 0; i < num; i++) {
        devices.push({ deviceId: "device" + i });
        if (devices.length == 100) {
            Registry.removeDevices(devices, false, ((i, err, b, c) => {
                if (err) {
                    console.log("error" + err.message);
                }
                console.log("finished with " + i);
            }).bind(this, i + 1));
            devices = [];
        }
    }
}

//addDevice(500);
// removeDevice(500);