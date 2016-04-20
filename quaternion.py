#! /usr/bin/env python

from math import sqrt, sin, cos, pi
#from copy import deepcopy
import pygame
import pygame.gfxdraw

pygame.init()

# Define the colors we will use in RGB format
BLACK = (  0,   0,   0)
WHITE = (255, 255, 255)
BLUE =  (  0,   0, 255)
GREEN = (  0, 255,   0)
RED =   (255,   0,   0)
YELLOW = (255, 255, 0)
MAGENTA = (255, 0, 255)
CYAN = (0, 255, 255)

# Set the height and width of the screen
size = [500, 500]
centre = [size[0]/2, size[1]/2]
screen = pygame.display.set_mode(size)

#Loop until the user clicks the close button.
done = False
clock = pygame.time.Clock()

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
    coord = ( rot * self * rot.conj() ).getvec()
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
  def __init__(self, nodeList, graphOfNodes, facesList = [], invisible = False):
    self.nodes = nodeList
    self.graph = graphOfNodes
    self.faces = facesList
    self.invis = invisible
  
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
    coords = [(centre[0] + scale*x, centre[1] + scale*y, z) for (x, y, z) in self.proj(projquat)]
    projFaces = sorted( self.faces, key = lambda face: min( [ coords[ pt ][2] for pt in face[0] ] ) )
    for edge in self.graph:
      pygame.draw.aaline(screen, BLACK, coords[edge[0]][:2], coords[edge[1]][:2])
    for face in projFaces:
      pygame.gfxdraw.filled_polygon(screen, [coords[pt][:2] for pt in face[0] ], face[1])
      pygame.gfxdraw.aapolygon(screen, [coords[pt][:2] for pt in face[0] ], BLACK)
    

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
    for shape in self.shapes:
      if not shape.invis: shape.draw(self.ax, self.sc)
      
  def rotate(self, rotquat):
    self.ax.rotate(rotquat)

def torad( degrees ): return degrees * pi/180




myquats = [ Node( (-1, -1, -1) ),
          Node( (-1, -1, 1) ),
          Node( (-1, 1, -1) ),
          Node( (-1, 1, 1) ),
          Node( (1, -1, -1) ),
          Node( (1, -1, 1) ),
          Node( (1, 1, -1) ),
          Node( (1, 1, 1) )]

cube = Shape(myquats, [ (0, 1), (0, 2), (0, 4), (1, 3), (1, 5), (2, 3), (2, 6), (3, 7), (4, 5), (4, 6), (5, 7), (6, 7)],
             [ ( [ 0, 2, 6, 4 ] , BLUE), ( [ 3, 1, 5, 7 ] , YELLOW ), ( [ 0, 1, 3, 2 ] , GREEN ), 
              ( [ 4, 5, 7, 6 ] , MAGENTA ), ( [ 0, 1, 5, 4 ] , RED ), ( [ 2, 3, 7, 6 ] , CYAN ) ])
cube.translate( Node( (-3, 1, 3) ) )

rotvec = (1,1,1)
myangle = torad(1)

centreToOrigin = Shape( [Node( (0, 0, 0) ), Node( (-3, 1, 3) )], [ (0, 1) ], invisible = True )
rotaxis = Shape( [Node( (-20, -20, -20) ), Node( (20, 20, 20) )], [ (0, 1) ] )

#myEnv = Environment((0.5,1,0.5), 30)
myEnv = Environment((0,1,-1), 30)
myEnv.addShape(cube)
myEnv.addShape(centreToOrigin)
#myEnv.addShape(rotaxis)

#myEnvRotAxis = (1, 0, 0)
#myEnvRotAngle = torad(0.05)
#myEnvRotQuat = Quaternion(0, myEnvRotAxis, True)
#myEnvRotQuat.setangle( myEnvRotAngle )


#projection = Quaternion(0, rotvec, True)

rotation = Quaternion(0, rotvec, True)
rotation.setangle( myangle )

rotvec2 = (1, 1, 1)
myangle2 = torad(3)
rotation2 = Quaternion(0, rotvec2, True)
rotation2.setangle( myangle2 )

index = 0
while not done:
 
    # This limits the while loop to a max of 10 times per second.
    # Leave this out and we will use all CPU we can.
    clock.tick(100)
     
    for event in pygame.event.get(): # User did something
        if event.type == pygame.QUIT: # If user clicked close
            done=True # Flag that we are done so we exit this loop
    
    screen.fill(WHITE)
    myEnv.draw()
    #for i in myEnv.shapes[0].nodes: print i
    pygame.display.flip()
    #pygame.image.save(screen, str(index).zfill(6)+'.png')
    myEnv.shapes[0].rotate(rotation)
    myEnv.shapes[1].rotate(rotation)
    #print myEnv.shapes[1].nodes[1]
    myEnv.shapes[0].rotate(rotation2, myEnv.shapes[1].nodes[1] )
    #myEnv.rotate( myEnvRotQuat )
    index += 1
    
pygame.quit()

#for quat in myquats: 
  #print '------'
  #print rotquat * quat * rotquat.conj()

  
#for myproj in myprojs: print myproj

#print '-----'

#for i in cube.proj(projection): print i