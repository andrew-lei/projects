function genRandom(low, high) {
  return low + Math.floor(Math.random()*(high - low));
}
function isPrime(number){
  //number assumed not even
  for ( var i = 3; i <= Math.sqrt(number); i += 2 ){
    if ( number%i == 0 ){
      return false;
    }
  }
  return true;
}
function findNextPrime(number) {
  nextPrime = number;
  if ( number%2 == 0 ){
    nextPrime++;
  }
  while ( !isPrime(nextPrime) ){
    nextPrime += 2;
  }
  return nextPrime;
}
function substrToNum(substr){
  var num = 0;
  if (substr.indexOf(String.fromCharCode(254)) == -1){
    var cutString = substr;
  }
  else{
    var cutString = substr.slice(0, substr.indexOf(String.fromCharCode(254)));
  }
  for ( var i = 0; i < cutString.length; i++ ){
    num += ( cutString.charCodeAt(i) - 31 ) * Math.pow(96, cutString.length - i - 1);
  }
  return num;
}
function numToSubstr(num, encrypted){
  var substr = '';
  var temp = num;
  //num < 96^n, log(num) < n log(96), n > log(num)/log(96)
  var n = Math.floor(Math.log(num)/Math.log(96));
  for ( var i = 0; i <= n; i++ ){
    substr += String.fromCharCode( Math.floor( ( num / Math.pow(96, n-i) ) % 96 + 31 ) );
  }
  if (encrypted){
    substr += Array( 11 - substr.length ).join(String.fromCharCode(254));
  }
  return substr;
}
function modMult(a, b, mod) {
    if (a == 0 || b == 0) {
        return 0;
    }
    if (a == 1) {
        return b;
    }
    if (b == 1) {
        return a;
    }

    var a2 = modMult(a, Math.floor(b / 2), mod);

    // Even factor
    if ((b % 2) == 0) {
      return (a2 + a2) % mod;
    }
    else {
      return ((a % mod) + (a2 + a2)) % mod;
    }
}
function encodePart(part, exponent, modulus){
  var result = 1;
  var base = part % modulus;
  var rexp = exponent;
  while (rexp > 0){
    if (rexp % 2 == 1){
      result = modMult(result, base, modulus);
    }
    rexp = Math.floor( rexp / 2 );
    base = modMult(base, base, modulus);
  }
  return result;
}
function encode(inString, exp, mod, decode){
  var splitvar;
  var workString = inString;
  if (!decode){
    workString += Array( ( ( 5 - inString.length % 5 ) + 1 ) % 6 ).join(' ');
    splitvar = 5;
  }
  else{
    splitvar = 10;
  }
  var encodedMessage = '';
  for ( var i = 0; splitvar * i < inString.length; i++ ){
    var toEncode = substrToNum(workString.slice( splitvar * i, splitvar * i + splitvar ));
    var encoded = encodePart(toEncode, exp, mod);
    encodedMessage += numToSubstr(encoded, !decode);
  }
  return encodedMessage;
}
function extEuclid(num, mod){
  var s = 0;
  var old_s = 1;
  var t = 1;
  var old_t = 0;
  var r = mod;
  var old_r = num;
  var quotient;
  var prov;
  while (r != 0){
    quotient = Math.floor( old_r / r );
    prov = r;
    r = old_r - quotient * prov;
    old_r = prov;
    prov = s;
    s = old_s - quotient * prov;
    old_s = prov;
    prov = t;
    t = old_t - quotient * prov;
    old_t = prov;
  }
  if ( old_s > 0 ){
    return old_s;
  }
  else {
    return old_s + mod;
  }
}
function encodeHTML(s) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

$(function()
{
  var num1, num2;
  var d = 1; //for some reason, the private key is computed by extEuclid as
  //one occasionally; God only knows why.
  $('#genkeys').click(function(){
    while (d == 1){
      num1 = findNextPrime( genRandom(1000000, 10000000) );
      num2 = findNextPrime( genRandom(1000000, 10000000) );
      while ( num1 == num2 ){
        num2 = findNextPrime( genRandom(1000000, 10000000) );
      }
      d = extEuclid(17, (num1 - 1) * (num2 - 1) );
    }
    document.getElementById('pub').value = num1 * num2;
    document.getElementById('priv').value = d;
  });
  $('#encrypt').click(function(){
    var publickey = parseInt(document.getElementById('pub').value);
    var message = document.getElementById('input').value;
    var encryptedMessage = encode(message, 17, publickey, false);
    document.getElementById('input2').value = encryptedMessage;
  });

  $('#decrypt').click(function(){
    var publickey = parseInt(document.getElementById('pub').value);
    var privatekey = parseInt(document.getElementById('priv').value);
    var message = document.getElementById('input2').value;
    var decryptedMessage = encode(message, privatekey, publickey, true);
    document.getElementById('decrypted').innerHTML = encodeHTML(decryptedMessage);
  });
});
