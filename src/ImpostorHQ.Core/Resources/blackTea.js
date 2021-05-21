"use strict";

//curtesy of anti :) 


//#region Constant
var delta = 0x9E3779B9;
var rounds = 32;
//#endregion

//#region Final Functions
function encrypt(dataStr, keyString) {
    var data = getBytes(dataStr);
    var keyBuffer = getKey(keyString);
    var blockBuffer = new Uint32Array(2);
    var blockLen = nextMultiple(data.length + 8);

    var encryptionBuffer = new Uint8Array(blockLen);
    var cipher = new Array();
    var lenBuffer = longToBytes(data.length);
    encryptionBuffer = arrayCopyTo(lenBuffer, encryptionBuffer, 0, 0, 8);
    encryptionBuffer = arrayCopyTo(data, encryptionBuffer, 0, 8, data.length);

    for (let i = 0; i < encryptionBuffer.length; i += 8) {
        blockBuffer[0] = bytesToDecimal(encryptionBuffer.slice(i, i + 4));
        blockBuffer[1] = bytesToDecimal(encryptionBuffer.slice(i + 4, i + 8));``
        blockBuffer = encryptBlock(blockBuffer, keyBuffer);
        var arr0 = decimalToBytes(blockBuffer[0]);
        var arr1 = decimalToBytes(blockBuffer[1]);
        for (var j = 0; j < 4; j++) {
            cipher.push(arr0[j]);
        }
        for (var j = 0; j < 4; j++) {
            cipher.push(arr1[j]);
        }
    }

    return toBase64(cipher);
};

function decrypt(dataB64, keyString) {
    var data = fromBase64(dataB64);
    var keyBuffer = getKey(keyString);
    var blockBuffer = new Uint32Array(2);
    var buffer = new Uint8Array(data.length);
    buffer = arrayCopyTo(data, buffer, 0, 0, data.length);
    var plain = new Array();

    for (let i = 0; i < buffer.length; i += 8) {
        blockBuffer[0] = bytesToDecimal(buffer.slice(i, i + 4));
        blockBuffer[1] = bytesToDecimal(buffer.slice(i + 4, i + 8));
        blockBuffer = decryptBlock(blockBuffer, keyBuffer);
        var arr0 = decimalToBytes(blockBuffer[0]);
        var arr1 = decimalToBytes(blockBuffer[1]);
        for (var j = 0; j < 4; j++) {
            plain.push(arr0[j]);
        }
        for (var j = 0; j < 4; j++) {
            plain.push(arr1[j]);
        }
    }
    var expectedLength = bytesToLong(plain.slice(0, 8));
    return getString(plain.slice(8, expectedLength + 8));
};
//#endregion

//#region Base64 Functions.

function fromBase64(str) {
    return Uint8Array.from(atob(str), c => c.charCodeAt(0));
};

function toBase64(bytes) {
    var strAbstractedBytes = getString(bytes);
    return btoa(strAbstractedBytes);
};
//#endregion

//#region BitConverter
function bytesToDecimal(byteArray) {
    var value = 0;
    for (var i = byteArray.length - 1; i >= 0; i--) {
        value = (value * 256) + byteArray[i];
    }
    return value;
};

function decimalToBytes(decimal) {
    var arr = new Uint8Array(4);
    for (var i = 0; i < 4; i++) {
        arr[i] = decimal & (255);
        decimal = decimal >> 8;
    }
    return arr;
};

function longToBytes(decimal) {
    var arr = new Uint8Array(8);
    for (var index = 0; index < 8; index++) {
        var byte = decimal & 0xff;
        arr[index] = byte;
        decimal = (decimal - byte) / 256;
    }

    return arr;
};

function bytesToLong(byteArray) {
    var value = 0;
    for (var i = byteArray.length - 1; i >= 0; i--) {
        value = (value * 256) + byteArray[i];
    }

    return value;
};

//#endregion

//#region Array Manipulation
function arrayCopyTo(source, destination, sourceIndex, destinationIndex, length) {
    for (var i = sourceIndex; i < length; i++) {
        destination[destinationIndex] = source[i];
        destinationIndex++;
    }
    return destination;
};
//#endregion

//#region Block Processing
function nextMultiple(length) {
    return Math.imul((((((length | 0) + 7) | 0) / 8) | 0), 8);
};

function decryptBlock(block, key) {
    var v0 = (block[0] >>> 0);
    var v = (block[1] >>> 0);
    var sum = (Math.imul(2654435769, rounds) >>> 0);

    for (var i = 0; i < rounds; i = ((i + 1) >>> 0)) {
        v = ((v - (((((v0 << 4) ^ (v0 >>> 5)) + v0) >>> 0) ^ ((sum + (key[(sum >>> 11) & 3] >>> 0)) >>> 0))) >>> 0);
        sum = ((sum - 2654435769) >>> 0);
        v0 = ((v0 - (((((v << 4) ^ (v >>> 5)) + v) >>> 0) ^ ((sum + (key[sum & 3] >>> 0)) >>> 0))) >>> 0);
    }
    block[0] = v0;
    block[1] = v;
    return block;
};

function encryptBlock(block, key) {
    var v0 = (block[0] >>> 0);
    var v = (block[1] >>> 0);
    var sum = 0;

    for (var i = 0; i < rounds; i = ((i + 1) >>> 0)) {
        v0 = ((v0 + (((((v << 4) ^ (v >>> 5)) + v) >>> 0) ^ ((sum + (key[sum & 3] >>> 0)) >>> 0))) >>> 0);
        sum = ((sum + 2654435769) >>> 0);
        v = ((v + (((((v0 << 4) ^ (v0 >>> 5)) + v0) >>> 0) ^ ((sum + (key[(sum >>> 11) & 3] >>> 0)) >>> 0))) >>> 0);
    }
    block[0] = v0;
    block[1] = v;
    return block;
};
//#endregion

//#region String Operations
function getString(array) {
    return String.fromCharCode.apply(String, array);
};

function getBytes(str) {
    var arr = new Uint8Array(str.length);
    for (var i = 0; i < str.length; i++) {
        arr[i] = str.charCodeAt(i);
    }
    return arr;
}
//#endregion

//#region Key Deriver
function getKey(password) {
    var hash = md51(password);
    hash[0] = Math.abs(hash[0]);
    hash[1] = Math.abs(hash[1]);
    hash[2] = Math.abs(hash[2]);
    hash[3] = Math.abs(hash[3]);
    return hash;
};
//#endregion