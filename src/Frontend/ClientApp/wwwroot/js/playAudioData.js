async function playAudioData(audioData) {
    var blob = new Blob([audioData], { type: "audio/wav" });
    var url = URL.createObjectURL(blob);
    var audio = new Audio(url);
    audio.play();
}  
