module EulerEE exposing (..)

import Html exposing (Attribute, br, div, text, input)
import Html.Attributes as H
import Html.Events exposing (onInput)
import String
import Svg exposing (Svg, linearGradient, stop)
import Svg.Attributes exposing (id, stroke, offset, stopColor, stopOpacity)
import Plot exposing (..)

acosh x = logBase e (x + sqrt (x*x - 1))
cosh x = (e^x + e^(-x)) / 2

eulerCubeRHS p x = p * (x^3 - x)
eulerCubeSolveLHS rhs =
  let
    acosarg = -1.5 * rhs * (sqrt 3)

    roots = if (acosarg <= 1) && (acosarg >= -1)
      then List.map (\k -> 2 * sqrt (1/3) * cos ((acos acosarg)/3 - 2 * pi * k / 3)) [0, 1, 2]
      else [-2 * (abs rhs) / rhs * (sqrt (1/3)) * cosh (1/3 * acosh (1.5 * (abs rhs) * sqrt 3))]
  in roots

eulerCubeLHS y = y^3 - y
eulerCubeSolveRHS p lhs = if p == 0 then []
  else let
    lhsp = lhs/p
    acosarg = -1.5 * (lhsp) * (sqrt 3)

    roots = if (acosarg <= 1) && (acosarg >= -1)
      then List.map (\k -> 2 * sqrt (1/3) * cos ((acos acosarg)/3 - 2 * pi * k / 3)) [0, 1, 2]
      else [-2 * (abs lhsp) / lhsp * (sqrt (1/3)) * cosh (1/3 * acosh (1.5 * (abs (lhsp)) * sqrt 3))]
  in roots

eulerCube : Float -> List (Float) -> List ( Float, Float )
eulerCube p xs = List.concatMap (\x -> ((eulerCubeSolveLHS << (eulerCubeRHS p)) x |> List.map (\y -> (x,y)))) xs ++
  List.concatMap (\y -> (((eulerCubeSolveRHS p) << eulerCubeLHS) y |> List.map (\x -> (x,y)))) xs

data : Float -> List ( Float, Float )
data p = List.range (-1000) 1000 |> List.map (\x -> (x |> toFloat) / 500 ) |> eulerCube p

circ1 = viewCircle 5.0 "#ff0000" |> dot
circ2 = viewCircle 0.5 "#0000ff" |> dot

fixed : Series (List ( Float, Float )) msg
fixed = (List.map (\(x, y) -> circ1 x y)) << (List.filter (\(x,y) -> List.member x [-1,0,1])) |> dots
allpoints = (List.map (\(x, y) -> circ2 x y)) |> dots

type alias Model = Float

type Msg
    = Update String

update : Msg -> Model -> Model
update (Update v) model =
    String.toFloat v |> Result.withDefault 0

view model =
  div []
    [ input
      [ H.type_ "range"
      , H.min "-2"
      , H.max "2"
      , H.step "0.01"
      , H.value <| toString model
      , onInput Update
      ] []
    , br [] []
    , text <| "y³ - y = " ++ toString model ++ " · (x³ - x)"
    , br [] []
    , data model |> viewSeriesCustom
        { defaultSeriesPlotCustomizations | toDomainLowest = \_ -> -2
                                          , toDomainHighest = \_ -> 2
                                          , toRangeLowest = \_ -> -2
                                          , toRangeHighest = \_ -> 2 }
        [ allpoints, fixed ]
    ]

main = Html.beginnerProgram
    { model = 0
    , view = view
    , update = update
    }
