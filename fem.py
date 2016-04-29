#!/usr/bin/env python

import sys
import json
import cgi
import numpy as np
from numpy import sinh, cos, pi

fs = cgi.FieldStorage()


#N = 21
Nx = int(fs.getvalue('Nx'))
Ny = int(fs.getvalue('Ny'))

dx = 2.0/(Nx - 1)
dy = 1.0/(Ny - 1)

boundaryl = [0 for i in range(Ny)]
boundaryr = [i*dy for i in range(Ny)]

def B2(xi,eta):
  mat = np.matrix([[-0.25*(1-eta),0.25*(1-eta),0.25*(1+eta),-0.25*(1+eta)],
                   [-0.25*(1-xi),-0.25*(1+xi),0.25*(1+xi),0.25*(1-xi)]])
  return mat

def Jacobian(xi,eta,xyvec):
  Jac = B2(xi,eta)*xyvec
  return Jac, np.linalg.det(Jac)

def B(xi,eta,xyvec):
  return np.linalg.inv(Jacobian(xi,eta,xyvec)[0])*B2(xi,eta)

def Kintegrand(xi,eta,kappa,xyvec):
  return kappa*B(xi,eta,xyvec).transpose()*B(xi,eta,xyvec)*Jacobian(xi,eta,xyvec)[1]

def K(kappa,xyvec):
  return sum([Kintegrand(xi,eta,kappa,xyvec) for xi in (-0.57735,0.57735) for eta in (-0.57735,0.57735)])

def el2gnn(i,alpha):
  if i == 0: return alpha / (Ny - 1) * Ny + alpha % (Ny - 1) + 1
  if i == 1: return (alpha / (Ny - 1) + 1) * Ny + alpha % (Ny - 1) + 1
  if i == 2: return (alpha / (Ny - 1) + 1) * Ny + alpha % (Ny - 1)
  if i == 3: return alpha / (Ny - 1) * Ny + alpha % (Ny - 1)

def gnn2gen(n):
  if n < Ny: return (Nx - 2)*Ny + n
  elif n < (Nx - 1) * Ny: return n - Ny
  else: return n

xy = np.matrix([[0, 0], [dx, 0], [dx, dy], [0, dy]])
Kmat = K(1, xy)
Ktot = np.zeros( (Nx * Ny, Nx * Ny) )

for alpha in range( (Nx - 1) * (Ny - 1) ):
  for i in range(4):
    for j in range(4):
      Ktot[ gnn2gen( el2gnn( i , alpha ) ), gnn2gen( el2gnn( j , alpha ) ) ] += Kmat[ i , j ]


Kff = Ktot[ :-2*Ny , :-2*Ny ]
Kfp = Ktot[ :-2*Ny , -2*Ny: ]
Up = np.matrix([boundaryl + boundaryr]).transpose()

Uf = np.linalg.solve(Kff, -Kfp*Up)

phi = np.array(np.concatenate( (np.matrix([boundaryl]).transpose(), np.reshape(Uf, (Ny, -1), order = 'F'), np.matrix([boundaryr]).transpose()), axis = 1))
result = phi.tolist()

#np.savetxt('../data.out', phi, delimiter = ',')
#datafile = open('../data.out', 'w')
#datafile.write(str(result))

sys.stdout.write("Content-Type: application/json")

sys.stdout.write("\n")
sys.stdout.write("\n")


result = {}
result['success'] = True
result['data'] = str(phi.tolist())

sys.stdout.write(json.dumps(result,indent=1))
sys.stdout.write("\n")

sys.stdout.close()