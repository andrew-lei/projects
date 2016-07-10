package org.andrewlei

import language.implicitConversions

//TODO: Handle infinite values?
class Dual( realval:Double, infinitesimal:Double ){
  //real and infinitesimal parts
  var real:Double = realval
  var inf:Double = infinitesimal

  //negative
  def unary_- = new Dual(-real, -inf)

  //basic calc arithmetic rules
  def +(operand:Dual):Dual = new Dual(real + operand.real, inf + operand.inf)
  def -(operand:Dual):Dual = new Dual(real - operand.real, inf - operand.inf)
  def *(operand:Dual):Dual = new Dual(real * operand.real, real * operand.inf + inf * operand.real)
  def /(operand:Dual):Dual = {
    //if (operand.real == 0.0)
    //  throw new ArithmeticException("/ by zero")
    new Dual(real / operand.real, (inf * operand.real - real * operand.inf)
              / (operand.real * operand.real))
  }

  def +=(operand:Dual):Unit = {
    real += operand.real
    inf += operand.inf
  }

  def -=(operand:Dual):Unit = {
    real -= operand.real
    inf -= operand.inf
  }

  def *=(operand:Dual):Unit = {
    inf = real * operand.inf + inf * operand.real
    real *= operand.real
  }

  def /=(operand:Dual):Unit = {
    if (operand.real == 0)
      throw new ArithmeticException("/ by zero")
    inf = (inf * operand.real - real * operand.inf) / (operand.real * operand.real)
    real /= operand.real
  }

  override def toString = real + "+" + inf + "\u03B5"
}

class Dual2( realval:Double, infval1:Double, infval2:Double ){
  //real and infinitesimal parts
  var real:Double = realval
  var inf1:Double = infval1
  var inf2:Double = infval2

  //negative
  def unary_- = new Dual2(-real, -inf1, -inf2)

  //basic calc arithmetic rules
  def +(operand:Dual2):Dual2 = new Dual2(real + operand.real, inf1 + operand.inf1, inf2 + operand.inf2)
  def -(operand:Dual2):Dual2 = new Dual2(real - operand.real, inf1 - operand.inf1, inf2 - operand.inf2)
  def *(operand:Dual2):Dual2 = new Dual2(real * operand.real, real * operand.inf1 + inf1 * operand.real,
                                         real * operand.inf2 + inf2 * operand.real + 2 * inf1 * operand.inf1)
  def /(operand:Dual2):Dual2 = {
    if (operand.real == 0.0)
      throw new ArithmeticException("/ by zero")
    new Dual2(real / operand.real, (inf1 * operand.real - operand.inf1 * real) / (operand.real * operand.real),
              ( operand.real * (operand.real * inf2 - real * operand.inf2) + (real * operand.inf1 - operand.real * inf1) *
              (2 * operand.inf1) ) / (operand.real * operand.real * operand.real) )
  }

  def +=(operand:Dual2):Unit = {
    real += operand.real
    inf1 += operand.inf1
    inf2 += operand.inf2
  }

  def -=(operand:Dual2):Unit = {
    real -= operand.real
    inf1 -= operand.inf1
    inf2 -= operand.inf2
  }

  def *=(operand:Dual2):Unit = {
    inf2 = real * operand.inf2 + inf2 * operand.real + 2 * inf1 * operand.inf1
    inf1 = real * operand.inf1 + inf1 * operand.real
    real *= operand.real
  }

  def /=(operand:Dual2):Unit = {
    if (operand.real == 0)
      throw new ArithmeticException("/ by zero")
    inf2 = ( operand.real * (operand.real * inf2 - real * operand.inf2) + (real * operand.inf1 - operand.real * inf1) *
           (2 * operand.inf1) ) / (operand.real * operand.real * operand.real)
    inf1 = (inf1 * operand.real - real * operand.inf1) / (operand.real * operand.real)
    real /= operand.real
  }

  override def toString = real + " + " + inf1 + "\u03B5\u2081 + " + inf1 + "\u03B5\u2082 + " + inf2 + "\u03B5\u2081\u03B5\u2082"
}

object Dual {
  implicit def Double2Dual(value:Double) = new Dual(value, 0.0)
  implicit def Double2Dual2(value:Double) = new Dual2(value, 0.0, 0.0)

  //trig functions
  def sin(x:Dual):Dual = new Dual(math.sin(x.real), math.cos(x.real) * x.inf)
  def cos(x:Dual):Dual = new Dual(math.cos(x.real), -math.sin(x.real) * x.inf)
  def tan(x:Dual):Dual = new Dual(math.tan(x.real), x.inf / ( math.cos(x.real) * math.cos(x.real) ) )
  def asin(x:Dual):Dual = new Dual(math.asin(x.real), x.inf / math.sqrt(1 - x.real * x.real) )
  def acos(x:Dual):Dual = new Dual(math.acos(x.real), -x.inf / math.sqrt(1 - x.real * x.real) )
  def atan(x:Dual):Dual = new Dual(math.atan(x.real), x.inf / (1 + x.real * x.real) )

  def sin(x:Dual2):Dual2 = new Dual2(math.sin(x.real), math.cos(x.real) * x.inf1,
                                    -math.sin(x.real) * x.inf1 * x.inf1 + math.cos(x.real) * x.inf2)
  def cos(x:Dual2):Dual2 = new Dual2(math.cos(x.real), -math.sin(x.real) * x.inf1,
                                    -math.cos(x.real) * x.inf1 * x.inf1 - math.sin(x.real) * x.inf2)
  def tan(x:Dual2):Dual2 = new Dual2(math.tan(x.real), x.inf1 / ( math.cos(x.real) * math.cos(x.real) ),
                                     ( x.inf2 * math.cos(x.real) - 2 * x.inf1 * x.inf1 * math.sin(x.real) ) * math.pow(math.cos(x.real), -3))
  def asin(x:Dual2):Dual2 = new Dual2(math.asin(x.real), x.inf1 / math.sqrt(1 - x.real * x.real),
                                      ( x.inf2 * (1 - x.real * x.real) + x.inf1 * x.inf1 + x.real ) * math.pow(1 - x.real, -1.5) )
  def acos(x:Dual2):Dual2 = new Dual2(math.acos(x.real), -x.inf1 / math.sqrt( 1 - x.real * x.real ),
                                      -( x.inf2 * (1 - x.real * x.real) + x.inf1 * x.inf1 + x.real ) * math.pow(1 - x.real, -1.5) )
  def atan(x:Dual2):Dual2 = new Dual2(math.atan(x.real), x.inf1 / (1 + x.real * x.real),
                                      ( x.inf2 * (1 + x.real * x.real) - 2 * x.inf1 * x.inf1 * x.real ) * math.pow((1 + x.real * x.real), -2) )


  //hyperbolic trig
  def sinh(x:Dual):Dual = new Dual(math.sinh(x.real), x.inf * math.cosh(x.real))
  def cosh(x:Dual):Dual = new Dual(math.cosh(x.real), x.inf * math.sinh(x.real))
  def tanh(x:Dual):Dual = new Dual(math.tanh(x.real), x.inf / ( math.cosh(x.real) * math.cosh(x.real) ) )
  /*def asinh(x:Dual):Dual = new Dual(math.asinh(x.real), x.inf / math.sqrt( x.real * x.real + 1.0 ) )
  def acosh(x:Dual):Dual = new Dual(math.acosh(x.real), x.inf / math.sqrt( x.real * x.real - 1.0 ) )
  def atanh(x:Dual):Dual = new Dual(math.atanh(x.real), x.inf / ( 1.0 - x.real * x.real ) )*/

  def sinh(x:Dual2):Dual2 = new Dual2(math.sinh(x.real), x.inf1 * math.cosh(x.real),
                                      x.inf2 * math.cosh(x.real) + x.inf1 * x.inf1 * math.sinh(x.real))
  def cosh(x:Dual2):Dual2 = new Dual2(math.cosh(x.real), x.inf1 * math.sinh(x.real),
                                      x.inf2 * math.sinh(x.real) + x.inf1 * x.inf1 * math.cosh(x.real))
  def tanh(x:Dual2):Dual2 = new Dual2(math.tanh(x.real), x.inf1 / ( math.cosh(x.real) * math.cosh(x.real) ),
                                      (x.inf2 * math.cosh(x.real) - 2 * math.sinh(x.real) * x.inf1 * x.inf1) * math.pow(math.cosh(x.real), -3) )


  //exp and log
  def exp(x:Dual):Dual = new Dual(math.exp(x.real), math.exp(x.real) * x.inf)
  //need to account for log when x.real == 0
  //should return Dual(-Infinity, Infinity)
  def log(x:Dual):Dual = new Dual(math.log(x.real), x.inf / x.real)

  def exp(x:Dual2):Dual2 = new Dual2(math.exp(x.real), math.exp(x.real) * x.inf1, math.exp(x.real) * (x.inf1 * x.inf1 + x.inf2) )
  def log(x:Dual2):Dual2 = new Dual2(math.log(x.real), x.inf1 / x.real, (x.real * x.inf2 - x.inf1 * x.inf1) / (x.real * x.real) )


  //power
  //pow might need some fixing, I believe this assumes both arguments
  //are functions of the same variable (although one can be constant)
  def pow(x:Dual, p:Dual):Dual = exp( p * log(x) )
  def pow(x:Dual, p:Double):Dual = new Dual(math.pow(x.real, p), p * math.pow(x.real, p - 1) * x.inf )
  def sqrt(x:Dual):Dual = new Dual(math.sqrt(x.real), 0.5 * x.inf / math.sqrt(x.real))

  def pow(x:Dual2, p:Dual2):Dual2 = exp( p * log(x) )
  def sqrt(x:Dual2):Dual2 = new Dual2(math.sqrt(x.real), 0.5 * x.inf1 / math.sqrt(x.real),
                                      (0.5 * x.real * x.inf2 - 0.25 * x.inf1 * x.inf1) * math.pow(x.real, -1.5) )


  //integral functions
  def erf(x:Dual):Dual = {
    val t = 1.0 / (1 + 0.5 * x.real.abs)
    val tau = t * math.exp(-x.real * x.real - 1.26551223 + 1.00002368 * t + 0.37409196 * t * t + 0.09678418 * math.pow(t, 3) -
              0.18628806 * math.pow(t,4) + 0.27886807 * math.pow(t,5) - 1.13520398 * math.pow(t,6) + 1.48851587 * math.pow(t, 7)
              -0.82215223 * math.pow(t,8) + 0.17087277 * math.pow(t,9) )
    if (x.real >= 0.0) new Dual(1 - tau, 2.0 / math.sqrt(math.Pi) * math.exp(-x.real * x.real) * x.inf )
    else new Dual(tau - 1, 2.0 / math.sqrt(math.Pi) * math.exp(-x.real * x.real) * x.inf )
  }

  //diff eq functions
  def legendre(x:Dual, n:Int):Dual = {
    if(n == 0)
      1
    def helper(add1:Dual, add2:Dual, n2:Int):Dual = {
      //n2 is a counter, we decrease it by 1 for each recursion when
      //it becomes 0, it should be add1, but I terminate it early for
      //n2 == 1 to save checking for that condition every pass through
      if(n2 == 1)
        add2
      else
        helper(add2, ( (2 * n2 - 1) * x * add2 - (n2 - 1) * add1 ) / n2, n2-1)
    }
    helper(1.0, x, n)
  }

  def legendre(x:Dual2, n:Int):Dual2 = {
    if(n == 0)
      1
    def helper(add1:Dual2, add2:Dual2, n2:Int):Dual2 = {
      //n2 is a counter, we decrease it by 1 for each recursion when
      //it becomes 0, it should be add1, but I terminate it early for
      //n2 == 1 to save checking for that condition every pass through
      if(n2 == 1)
        add2
      else
        helper(add2, ( (2 * n2 - 1) * x * add2 - (n2 - 1) * add1 ) / n2, n2-1)
    }
    helper(1.0, x, n)
  }

  //multiple root finder for Legendre polynomial
  //see: http://www.kurims.kyoto-u.ac.jp/EMIS/journals/AMI/2006/barrera.pdf
  def legendreRoots(degree:Int, tolerance:Double = 1e-12):List[Double] = {
    val zeros = collection.mutable.ListBuffer.empty[Double]
    var guess = new Dual(-1.0, 1.0)
    var i = 0
    while( i < degree / 2 ){
      var legvalue = legendre(guess, degree)
      while(legvalue.real.abs > tolerance){
        //update root x_i
        //x_i = x_i - f(x_i) / (f'(x_i) - f(x_i) * sum_{j < i}(1 / (x_i - x_j)))
        guess.real -= legvalue.real / (legvalue.inf - legvalue.real * zeros.map(x => 1.0 / (guess.real - x) ).sum)
        legvalue = legendre(guess, degree)
      }
      zeros += guess.real
      //this is a bad hack
      //but the best I can think of right now
      guess.real *= 0.999
      guess.inf = 1.0
      i += 1
    }
    zeros ++= zeros.map(x => -x)
    if(degree%2 == 1)
      zeros += 0.0
    zeros.toList
  }

  //computes weights for Gauss-Legendre quadrature
  //see: https://en.wikipedia.org/wiki/Gaussian_quadrature#Gauss.E2.80.93Legendre_quadrature
  def legendreWeights(degree:Int, zeros:List[Double]) = zeros.map(x => 2.0 / ( (1 - x*x) * math.pow(legendre(new Dual(x, 1.0), degree).inf, 2.0) ))

  //see: https://en.wikipedia.org/wiki/Gaussian_quadrature#Change_of_interval
  //this fails somewhere between degree = 100 and degree = 200
  //dunno why
  def gaussQuad(integrand: Double => Double, degree:Int, lower:Double, upper:Double) = {
    val zeros = legendreRoots(degree)
    val weights = legendreWeights(degree, zeros)
    (upper - lower) / 2.0 * (zeros zip weights).map( x => x._2 * integrand( (upper - lower) / 2.0 * x._1 + (lower + upper) / 2.0 )).sum
  }
}
