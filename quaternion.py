#! /usr/bin/env python

from math import sqrt, sin, cos, pi
import json
import cgi
import sys

class Quaternion:
  def __init__(self, realpart, (ipart, jpart, kpart), normalise = False):
    self.r = float( realpart )
    self.i = float( ipart )
    self.j = float( jpart )
    self.k = float( kpart )
    if normalise == True:
      #this makes the ijk part unit length
      #should be true for rotation quaternion
      length = sqrt( ipart**2 + jpart**2 + kpart**2 )
      self.i /= length
      self.j /= length
      self.k /= length

  #add two quaternions
  def __add__(self, quat2):
    #if we're adding a real number
    if isinstance(quat2, int) or isinstance(quat2, float):
      return Quaternion( self.r + quat2, (self.i, self.j, self.k) )
    #if it's a quaternion
    else: 
      return Quaternion( self.r + quat2.r, (self.i + quat2.i, self.j + quat2.j, self.k + quat2.k))

  def __div__(self, number):
    return Quaternion( self.r / number , (self.i / number, self.j / number, self.k / number) )

  #quaternion multiplication
  def __mul__(self, quat2):
    realpart = self.r * quat2.r - self.i * quat2.i - self.j * quat2.j - self.k * quat2.k
    ipart = self.r * quat2.i + self.i * quat2.r + self.j * quat2.k - self.k * quat2.j
    jpart = self.r * quat2.j - self.i * quat2.k + self.j * quat2.r + self.k * quat2.i
    kpart = self.r * quat2.k + self.i * quat2.j - self.j * quat2.i + self.k * quat2.r
    return Quaternion( realpart, (ipart, jpart, kpart) )

  #return negative quaternion
  #negging nonabelian-ly
  def __neg__(self):
    return Quaternion( -self.r, (-self.i, -self.j, -self.k) )

  #subtracts two quaternions
  def __sub__(self, quat2):
    #if we're subtracting a real number
    if isinstance(quat2, int) or isinstance(quat2, float):
      return Quaternion( self.r - quat2, (self.i, self.j, self.k) )
    #if quaternion
    else: 
      return Quaternion( self.r - quat2.r, (self.i - quat2.i, self.j - quat2.j, self.k - quat2.k))

  def __str__(self):
    return str( (self.r, (self.i, self.j, self.k) ) )

  #complex conjugate
  #keep real part fixed; imaginary parts times minus one
  def conj(self): 
    return Quaternion( self.r, (-self.i, -self.j, -self.k) )

  #get vector component
  def getvec(self):
    return (self.i, self.j, self.k)
  
  #this part is to set the angle for a rotation quaternion
  def setangle(self, theta):
    self.r = cos( theta / 2.0)
    self.i *= sin( theta / 2.0)
    self.j *= sin( theta / 2.0)
    self.k *= sin( theta / 2.0)

  #returns the rotation quaternion needed to rotate to quat
  def findrot(self, quat):
    rot = (-self * quat).conj() + 1
    length = sqrt(rot.r**2 + rot.i**2 + rot.j**2 + rot.k**2)
    rot /= length
    return rot

origin = Quaternion(0, (0,0,0) )

class Node(Quaternion):
  def __init__(self, (ipart, jpart, kpart), normalise = False):
    Quaternion.__init__(self, 0, (ipart, jpart, kpart), normalise)

  def __str__(self):
    return str( (self.i, self.j, self.k) )

  def proj(self, projquat):
    rot = projquat.findrot( Node( (0,0,1) ) )
    coord = ( rot * self * rot.conj() ).getvec()[:2]
    return coord
  
  #rotates node
  def rotate(self, rotquat, axispos = origin):
    rotatedNode = rotquat * ( self - axispos ) * rotquat.conj() + axispos
    self.i = rotatedNode.i
    self.j = rotatedNode.j
    self.k = rotatedNode.k

  def translate(self, location):
    self.i += location.i
    self.j += location.j
    self.k += location.k

class Shape:
  def __init__(self, nodeList, graphOfNodes):
    self.nodes = nodeList
    self.graph = graphOfNodes
  
  #rotates shape
  def rotate(self, rotquat, axispos = Node( (0,0,0) )):
    for node in self.nodes:
      node.rotate(rotquat, axispos)

  def proj(self, projquat):
    coords = []
    for node in self.nodes:
      coords += [ node.proj(projquat) ]
    return coords

  def draw(self, projquat, scale):
    coords = [[250 + scale*x, 250 + scale*y] for (x, y) in self.proj(projquat)]
    edges = []
    for edge in self.graph:
      edges += [ [ coords[edge[0]], coords[edge[1]] ] ]
    return edges

  def translate(self, location):
    for node in self.nodes:
      node.translate(location)
  
class Environment:
  def __init__(self, axis, scalefact):
    self.ax = Node(axis, True)
    self.sc = scalefact
    self.shapes = []

  def addShape(self, someShape):
    self.shapes += [someShape]

  def draw(self):
    result = {}
    result['shapes'] = str([ shape.draw(self.ax, self.sc) for shape in self.shapes ])
    result['axis'] = str(self.ax)
    result['success'] = True
    
    sys.stdout.write('Content-Type: application/json')
    sys.stdout.write('\n')
    sys.stdout.write('\n')
    
    sys.stdout.write(json.dumps(result,indent=1))
    sys.stdout.write('\n')
    
    sys.stdout.close()

def torad( degrees ): return degrees * pi/180


fs = cgi.FieldStorage()

orbitAxisAngle = ( torad( float(fs.getvalue('orbitPolar')) ) , torad( float(fs.getvalue('orbitAzimuth')) ))
orbitAxis = ( sin(orbitAxisAngle[0]) * cos(orbitAxisAngle[1]), 
             sin(orbitAxisAngle[0]) * sin(orbitAxisAngle[1]), cos(orbitAxisAngle[0]) )

spinAxisAngle = ( torad( float(fs.getvalue('spinPolar')) ) , torad( float(fs.getvalue('spinAzimuth')) ) )
spinAxis = ( sin(spinAxisAngle[0]) * cos(spinAxisAngle[1]), 
            sin(spinAxisAngle[0]) * sin(spinAxisAngle[1]), cos(spinAxisAngle[0]) )

viewAxisAngle = ( torad( float(fs.getvalue('viewPolar')) ) , torad( float(fs.getvalue('viewAzimuth')) ) )
viewAxis = ( sin(viewAxisAngle[0]) * cos(viewAxisAngle[1]), 
            sin(viewAxisAngle[0]) * sin(viewAxisAngle[1]), cos(viewAxisAngle[0]) )


orbitAngle = torad( float( fs.getvalue('orbitAngle') ) )
spinAngle = torad( float( fs.getvalue('spinAngle') ) )
zoom = float( fs.getvalue('zoom') )

#sys.stdout.write("Content-Type: application/json")

#sys.stdout.write("\n")
#sys.stdout.write("\n")


#result = {}
#result['success'] = True
#result['data'] = str( orbitAxisAngle )

#sys.stdout.write(json.dumps(result,indent=1))
#sys.stdout.write("\n")

#sys.stdout.close()


myquats = [ Node( (-1, -1, -1) ),
          Node( (-1, -1, 1) ),
          Node( (-1, 1, -1) ),
          Node( (-1, 1, 1) ),
          Node( (1, -1, -1) ),
          Node( (1, -1, 1) ),
          Node( (1, 1, -1) ),
          Node( (1, 1, 1) )]

cube = Shape(myquats, [ (0, 1), (0, 2), (0, 4), (1, 3), (1, 5), (2, 3), (2, 6), (3, 7), (4, 5), (4, 6), (5, 7), (6, 7)])
cube.translate( Node( (-3, 1, 3) ) )

centreToOrigin = Shape( [Node( (0, 0, 0) ), Node( (-3, 1, 3) )], [ (0, 1) ] )

myEnv = Environment(viewAxis, zoom)
myEnv.addShape(cube)
myEnv.addShape(centreToOrigin)

rotation = Quaternion(0, orbitAxis, True)
rotation.setangle( orbitAngle )

rotation2 = Quaternion(0, spinAxis, True)
rotation2.setangle( spinAngle )

myEnv.shapes[0].rotate(rotation)
myEnv.shapes[1].rotate(rotation)
myEnv.shapes[0].rotate(rotation2, myEnv.shapes[1].nodes[1])

myEnv.draw()
