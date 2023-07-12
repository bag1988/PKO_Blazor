// Set up media stream constant and parameters.
var mediaStreamConstraints;

// Set up to exchange only video.
const offerOptions = {
    offerToReceiveAudio: 1,
    offerToReceiveVideo: 1
};



//const servers = {
//    iceServers: [
//        {
//            urls: "turn:coturn.myserver.com:3478",
//            username: "username",
//            credential: "password"
//        }
//    ]
//}
const servers = null;

const peerConnectionArray = new Map();

let dotNet;
let localStream;

let localVideo;
let remoteVideoArray;



export function initialize(dotNetRef, _localVideo, _remoteVideoArray) {
    dotNet = dotNetRef;
    localVideo = _localVideo;
    remoteVideoArray = _remoteVideoArray;
}

function getMedia(constraints) {
    return new Promise((resolve) => {
        navigator.mediaDevices.getUserMedia(constraints)
            .then((mediaStream) => {
                resolve(mediaStream);
            })
            .catch(function (err) {
                console.log(err.name + ": " + err.message);
            });
    });
}

function startLocalStream() {
    return new Promise(async (resolve) => {
        if (!localStream)
            localStream = await getMedia(mediaStreamConstraints);

        if (localVideo && localStream) {
            localVideo.srcObject = localStream;
            localVideo.classList.remove("d-none");
        }
        resolve(localStream);
    });
}

async function createPeerConnection(forUrl) {

    console.log("Create P2P ", forUrl);

    if (peerConnectionArray.has(forUrl)) return;

    // Create peer connections and add behavior.
    var newPeerConnection = "hello";
    newPeerConnection = new RTCPeerConnection(servers);

    //Created local peer connection object peerConnection.
    newPeerConnection.addEventListener("icecandidate", (event) => { handleConnection(event, forUrl) });
    newPeerConnection.addEventListener("iceconnectionstatechange", (event) => { handleConnectionChange(event, forUrl) });
    newPeerConnection.addEventListener("addstream", (event) => { gotRemoteMediaStream(event, forUrl) });

    await startLocalStream();
    // Add local stream to connection and create offer to connect.    
    localStream.getTracks().forEach((track) => {
        newPeerConnection.addTrack(track, localStream);
    });
    peerConnectionArray.set(forUrl, newPeerConnection);

}

export async function callAction(forUrl, isvideo = false) {
    try {
        console.log(" >= callAction", forUrl);

        if (peerConnectionArray.has(forUrl)) return Promise.resolve();

        mediaStreamConstraints = {
            audio: true,
            video: isvideo
        };

        await createPeerConnection(forUrl);

        if (!peerConnectionArray.has(forUrl)) {
            console.log("error get peerConnection for", forUrl);
            return;
        }


        let offerDescription = await peerConnectionArray.get(forUrl).createOffer(offerOptions);

        await peerConnectionArray.get(forUrl).setLocalDescription(offerDescription);

        return JSON.stringify(offerDescription);
    }
    catch (e) {
        console.log(e);
    }
    return null;
}

// Signaling calls this once an answer has arrived from other peer. Once
// setRemoteDescription is called, the addStream event trigger on the connection.
export async function processAnswer(descriptionText, forUrl) {

    console.log(" => processAnswer: peerConnection setRemoteDescription start.", forUrl ?? "error");

    if (!peerConnectionArray.has(forUrl)) return;

    let description = JSON.parse(descriptionText);

    await peerConnectionArray.get(forUrl).setRemoteDescription(description);

    console.log("peerConnection.setRemoteDescription(description) success", forUrl);
}

//Создаем ответ для подключения
export async function processOffer(descriptionText, nameRoom, forUrl, isvideo = false) {
    try {
        console.log(" >= processOffer", forUrl);

        if (!peerConnectionArray.has(forUrl)) {
            mediaStreamConstraints = {
                audio: true,
                video: isvideo
            };

            await createPeerConnection(forUrl);
        }

        if (!peerConnectionArray.has(forUrl)) {
            console.log("error get peerConnection for", forUrl);
            return;
        }


        let description = JSON.parse(descriptionText);
        console.log("peerConnection setRemoteDescription start.", forUrl);
        await peerConnectionArray.get(forUrl).setRemoteDescription(description);

        console.log("peerConnection createAnswer start.", forUrl);
        let answer = await peerConnectionArray.get(forUrl).createAnswer();
        console.log("peerConnection setLocalDescription start.", forUrl);
        await peerConnectionArray.get(forUrl).setLocalDescription(answer);

        console.log("=> dotNet SendAnswer.", forUrl);
        dotNet.invokeMethodAsync("SendAnswerJs", nameRoom, JSON.stringify(answer), forUrl);
    }
    catch (e) {
        console.log(e);
        closePeerConnection(forUrl);
        dotNet.invokeMethodAsync("SendAnswerJs", nameRoom, "", forUrl);
    }
}

export async function processCandidate(candidateText, forUrl) {

    console.log(" >= processCandidate: peerConnection addIceCandidate start.", forUrl ?? "error");
    if (!peerConnectionArray.has(forUrl)) return;

    let candidate = JSON.parse(candidateText);

    await peerConnectionArray.get(forUrl).addIceCandidate(candidate);
    console.log("addIceCandidate added.", forUrl);
}

// Handles hangup action: ends up call, closes connections and resets peers.
export function closePeerConnection(forUrl) {

    if (peerConnectionArray.has(forUrl)) {
        peerConnectionArray.get(forUrl).close();
        peerConnectionArray.delete(forUrl);
        console.log(forUrl, "Ending call. P2P active ", peerConnectionArray.size);
    }

    if (remoteVideoArray) {
        console.log("remove video for url ", forUrl);
        remoteVideoArray.querySelector('[for="' + forUrl + '"]')?.remove();

        if (remoteVideoArray.querySelectorAll("video").length == 0) {
            remoteVideoArray.classList.add("d-none");            
        }
        if (remoteVideoArray.querySelectorAll("video").length < 4) {
            remoteVideoArray.classList.remove("row-cols-2");
        }
    }

    //если нет активных подключений, закрываем локальный стрим
    if (peerConnectionArray.size == 0 && localStream) {
        if (localVideo) {
            localVideo.srcObject = null;
            localVideo.classList.add("d-none");
            localStream.getTracks().forEach(track => {
                track.stop();
            });
            localStream = null;
        }
    }

}

// Handles remote MediaStream success by handing the stream to the blazor component.
function gotRemoteMediaStream(event, forUrl) {
    console.log("Set remote stream", forUrl);

    if (remoteVideoArray) {
        var remoteVideo = document.createElement("video");
        remoteVideo.autoplay = true;

        remoteVideo.poster = "/novideo.svg";

        remoteVideo.classList.add("col");
        remoteVideo.classList.add("p-2");
        remoteVideo.setAttribute("for", forUrl);

        remoteVideo.srcObject = event.stream;

        remoteVideoArray.append(remoteVideo);
        remoteVideoArray.classList.remove("d-none");

        if (remoteVideoArray.querySelectorAll("video").length > 3) {
            remoteVideoArray.classList.add("row-cols-2");
        }

    }

}

// Sends candidates to peer through signaling.
async function handleConnection(event, forUrl) {
    const iceCandidate = event.candidate;
    if (iceCandidate) {
        console.log(" => SendCandidate for ", forUrl);
        await dotNet.invokeMethodAsync("SendCandidateJs", JSON.stringify(iceCandidate), forUrl);
    }
}

// Logs changes to the connection state.
function handleConnectionChange(event, forUrl) {
    const peerConnection = event.target;
    console.log(`${forUrl} peerConnection ICE state: ${peerConnection.iceConnectionState}.`);

    //if (peerConnection.iceConnectionState == 'disconnected' || peerConnection.iceConnectionState == 'closed') {
    //    closePeerConnection(forUrl);
    //}

}


