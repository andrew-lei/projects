package org.andrewlei

object Parser {
  import Dual._

  def infix2RPN(input:String):collection.mutable.Queue[String] = {
    val operatorStack = collection.mutable.Stack[String]()
    val output = collection.mutable.Queue[String]()
    var i = 0

    while(i < input.length){
      if( (48 <= input(i).toByte && input(i).toByte <= 57) || input(i) == '.'){
        var j = i + 1
        while( j < input.length && ( (48 <= input(j).toByte && input(j).toByte <= 57) || input(j) == '.') ){
          j += 1
        }
        output.enqueue(input.substring(i, j))
        i = j - 1
      }
      else if(input(i) == 'x'){
        output.enqueue(input(i).toString)
      }
      else if( 97 <= input(i).toByte && input(i).toByte <= 122 ){
        var j = i + 1
        while( j < input.length && input(j) != '(' ){
          j += 1
        }
        operatorStack.push(input.substring(i, j))
        i = j - 1
      }
      else if(input(i) == '+' || input(i) == '-' || input(i) == '*' || input(i) == '/' || input(i) == '^' || input(i) == '('){
        if(input(i) == '*' || input(i) == '/'){
          while(!operatorStack.isEmpty && (operatorStack.top == "^" || operatorStack.top == "*" || operatorStack.top == "/") ){
            output.enqueue(operatorStack.pop)
          }
        }
        else if(input(i) == '+' || input(i) == '-'){
          while(!operatorStack.isEmpty && (operatorStack.top == "^" || operatorStack.top == "*" || operatorStack.top == "/" || operatorStack.top == "+" || operatorStack.top == "-") ){
            output.enqueue(operatorStack.pop)
          }
        }
        operatorStack.push(input(i).toString)
      }
      else if(input(i) == ')'){
        while(operatorStack.top != "("){
          output.enqueue(operatorStack.pop)
        }
        operatorStack.pop
      }

      i += 1
    }
    while(!operatorStack.isEmpty){
      output.enqueue(operatorStack.pop)
    }

    output
  }

  def computeRPN(input:collection.mutable.Queue[String]):Dual=>Dual = {
    x => {
      val func = input.clone
      val stack = collection.mutable.Stack[Dual]()
      while(!func.isEmpty){
        func.dequeue match {
          case it if 48 to 57 contains it(0).toByte => stack.push(it.toDouble)
          case "x" => stack.push(x)
          case "+" => stack.push(stack.pop + stack.pop)
          case "-" => stack.push(-stack.pop + stack.pop)
          case "*" => stack.push(stack.pop * stack.pop)
          case "/" => stack.push( ((a:Dual, b:Dual) => b/a)(stack.pop, stack.pop) )
          case "^" => stack.pop match {
            case it if it.inf == 0.0 => stack.push( pow(stack.pop, it.real) )
            case default => stack.push( pow(stack.pop, default) )
          }
          case "sin" => stack.push( sin(stack.pop) )
          case "cos" => stack.push( cos(stack.pop) )
          case "tan" => stack.push( tan(stack.pop) )
          case "asin" => stack.push( asin(stack.pop) )
          case "acos" => stack.push( acos(stack.pop) )
          case "atan" => stack.push( atan(stack.pop) )
          case "sinh" => stack.push( sinh(stack.pop) )
          case "cosh" => stack.push( cosh(stack.pop) )
          case "tanh" => stack.push( tanh(stack.pop) )
          case "exp" => stack.push( exp(stack.pop) )
          case "log" => stack.push( log(stack.pop) )
          case "sqrt" => stack.push( sqrt(stack.pop) )
          case "erf" => stack.push( erf(stack.pop) )
        }
      }
      stack.pop
    }
  }

  //Don't use this
  //
  // def evalRange(func:Dual=>Dual, lower:Double, upper:Double, interval:Double):(List[Double], List[Double], List[Double]) = {
  //   val List(x, y, yp) = (lower to upper by interval).
  //     map( t => ( (s:Dual) => List(t, s.real, s.inf) )(func( new Dual(t, 1.0) )) ).
  //     filter(t => !t(1).isNaN && !t(1).isInfinite && !t(2).isNaN && !t(2).isInfinite ).
  //     toList.
  //     transpose
  //   (x, y, yp)
  // }

  def computeRPNRange(input:collection.mutable.Queue[String]):(Double, Double, Double)=>(List[List[Double]]) = {
    (lower, upper, interval) => {
      val x = (lower to upper by interval).toList
      val xvar = x.map( t => new Dual(t, 1.0) )
      val func = input.clone
      val stack = collection.mutable.Stack[List[Dual]]()
      while(!func.isEmpty){
        func.dequeue match {
          case it if 48 to 57 contains it(0).toByte => stack.push(List.fill(x.length)(it.toDouble))
          case "x" => stack.push(xvar)
          case "+" => stack.push( (stack.pop zip stack.pop).map(t => t._1 + t._2 ) )
          case "-" => stack.push( (stack.pop zip stack.pop).map(t => t._2 - t._1 ) )
          case "*" => stack.push( (stack.pop zip stack.pop).map(t => t._1 * t._2 ) )
          case "/" => stack.push( (stack.pop zip stack.pop).map(t => t._2 / t._1 ) )
          case "^" => stack.
            push( (stack.pop zip stack.pop).map( t =>
              ( (a:Dual, b:Dual) =>
                if(b.inf == 0) pow(a, b.real)
                else pow(a, b)
              )(t._2, t._1)
            )
          )
          case "sin" => stack.push( stack.pop.map(t => sin(t)) )
          case "cos" => stack.push( stack.pop.map(t => cos(t)) )
          case "tan" => stack.push( stack.pop.map(t => tan(t)) )
          case "asin" => stack.push( stack.pop.map(t => asin(t)) )
          case "acos" => stack.push( stack.pop.map(t => acos(t)) )
          case "atan" => stack.push( stack.pop.map(t => atan(t)) )
          case "sinh" => stack.push( stack.pop.map(t => sinh(t)) )
          case "cosh" => stack.push( stack.pop.map(t => cosh(t)) )
          case "tanh" => stack.push( stack.pop.map(t => tanh(t)) )
          case "exp" => stack.push( stack.pop.map(t => exp(t)) )
          case "log" => stack.push( stack.pop.map(t => log(t)) )
          case "sqrt" => stack.push( stack.pop.map(t => sqrt(t)) )
          case "erf" => stack.push( stack.pop.map(t => erf(t)) )
        }
      }
      val result = stack.pop.map( t => List(t.real, t.inf) ).transpose
      List(x, result(0), result(1)).
        transpose.
        filter( x => !x(1).isNaN && !x(1).isInfinite && !x(2).isNaN && !x(2).isInfinite  ).
        transpose
    }
  }
}
