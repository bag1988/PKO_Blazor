﻿class RecorderProcessor extends AudioWorkletProcessor {
    static get parameterDescriptors() {
        return [{
            name: 'isRecording',
            defaultValue: 0
        }
        ];
    }

    // 0. Определите размер буфера (это то же самое, что и 1-й аргумент Script Processor) должено быть кратно 128
    _bufferSize = 8192;
    // 1. Отслеживание текущего уровня заполнения буфера
    _bytesWritten = 0;

    // 2. Создайте буфер фиксированного размера
    _buffers = null;

    _Channels = 1;


    constructor(options) {
        super();        
        this._bufferSize = Math.ceil(options.processorOptions.SampleRate / 128) * 128;
        this._buffers = null;
        this._Channels = options.processorOptions.ChannelCount;
        this._initBuffer();        
    }


    _initBuffers(numberOfChannels) {        
        this._buffers = [];
        for (let channel = 0; channel < numberOfChannels; channel++) {
            this._buffers.push(new Float32Array(this._bufferSize));
        }
    }


    _initBuffer() {
        this._bytesWritten = 0;
    }

    _isBufferEmpty() {
        return this._bytesWritten === 0;
    }

    _isBufferFull() {
        return this._bytesWritten === this._bufferSize;
    }

   
    process(inputs, outputs, parameters) {  

        const isRecordingValues = parameters.isRecording;
        const numberOfChannels = this._Channels;        
        if (this._buffers === null) {
            this._initBuffers(numberOfChannels)
        }
        
        for (let dataIndex = 0; dataIndex < isRecordingValues.length; dataIndex++) {            
            const shouldRecord = isRecordingValues[dataIndex] === 1;
            if (!shouldRecord && !this._isBufferEmpty()) {                
                this._flush();
                this._recordingStopped();                
            }

            if (shouldRecord) {
                let audioRawData = inputs[0];                
                this._pushToBuffers(audioRawData, numberOfChannels);
            }
        }
        return true;
    }
    

    _pushToBuffers(audioRawData, numberOfChannels) {
        if (this._isBufferFull()) {
            this._flush();
        }
        let dataLength = audioRawData[0].length;

        for (let idx = 0; idx < dataLength; idx++) {
            for (let channel = 0; channel < numberOfChannels; channel++) {                
                let value = audioRawData[channel][idx];
                this._buffers[channel][this._bytesWritten] = value;
            }
            this._bytesWritten += 1;
        }

        
    }

    _flush() {
        let buffers = [];
        this._buffers.forEach((buffer, channel) => {
            if (this._bytesWritten < this._bufferSize) {
                buffer = buffer.slice(0, this._bytesWritten);
            }
            buffers[channel] = buffer;
        });

        this.port.postMessage({
            eventType: 'data',
            audioBuffer: buffers,
            bufferSize: this._bufferSize
        });
        this._initBuffer();
    }

    _recordingStopped() {
        this.port.postMessage({
            eventType: 'stop'
        });
    }
}

registerProcessor('recorder.worklet', RecorderProcessor);