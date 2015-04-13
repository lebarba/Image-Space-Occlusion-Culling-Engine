using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer;

namespace Examples.OcclusionMap
{
    /// <summary>
    /// Viewport con dimensiones utilizadas para el DepthBuffer del OcclusionMap
    /// </summary>
    public class OcclusionViewport
    {
        Viewport d3dViewport;
        /// <summary>
        /// Viewport de DirectX
        /// </summary>
        public Viewport D3dViewport
        {
            get { return d3dViewport; }
        }

        Matrix projection;
        /// <summary>
        /// Matriz de proyeccion adaptada al viewport utilizado
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
        }

        Matrix view;
        /// <summary>
        /// Matriz de view
        /// </summary>
        public Matrix View
        {
            get { return view; }
            set { view = value; }
        }

        /// <summary>
        /// Distancia del near plane
        /// </summary>
        public const float nearPlaneDistance = 1f;

        public OcclusionViewport(int width, int height)
        {
            //Crear viewport
            d3dViewport = new Viewport();
            d3dViewport.X = 0;
            d3dViewport.Y = 0;
            d3dViewport.Width = width;
            d3dViewport.Height = height;
            d3dViewport.MinZ = 0f;
            d3dViewport.MaxZ = 1f;

            //Crear matriz de proyeccion
            float aspectRatio = (float)width / height;
            projection = Matrix.PerspectiveFovLH(FastMath.ToRad(45.0f), aspectRatio, nearPlaneDistance, 10000f);

            view = Matrix.Identity;
        }






        public Vector3[] projectQuad(Vector3[] p)
        {
            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Height;
            const float EPSILON_EQUALS_LENGTH = 1f;

            //Transformar a View space
            Vector4[] pView = new Vector4[4];
            for (int i = 0; i < 4; i++)
            {
                pView[i] = Vector3.Transform(p[i], view);
            }

            //Clipping
            List<Vector3> output = new List<Vector3>();
            int last = 3;
            for (int i = 0; i < 4; i++)
            {
                Vector4 p0 = pView[last];
                Vector4 p1 = pView[i];

                // 2- Clipping solo por near plane, quiero evitar que una recta pase por detras del near plane 
                // asi elimino los w<0
                // P0,P1 = linea en espacio de la camara 
                // Le paso estos 2 puntos a la funcion clip_line que las clippea en 3d por el near plane: 
                // devuelve la cantidad de puntos por delante del near plane y llegado el caso los corta  
                // la recta en el pto de interseccion con el near plane. 
                if (clipLine(ref p0, ref p1, nearPlaneDistance) > 0)
                {
                    // Hay segmento: 
                    // Transformo al espacio de proyeccion
                    Vector4 pOut = Vector4.Transform(p0, this.projection);

                    // Lo paso a coordenadas de pantalla: 
                    Vector3 pScreen = toScreenSpace(pOut, width, height);

                     // Agrego el punto P0, solo si es distinto al anterior (o no hay anterior)
                    if (output.Count == 0 || Vector3.LengthSq(output[output.Count - 1] - pScreen) > EPSILON_EQUALS_LENGTH)
                    {
                        output.Add(pScreen);
                    }

                    // Idem para el otro punto:
                    pOut = Vector4.Transform(p1, this.projection);
                    pScreen = toScreenSpace(pOut, width, height);

                    //Agrego el punto p1
                    if (i == 3)
                    {
                        if (Vector3.LengthSq(output[0] - pScreen) > EPSILON_EQUALS_LENGTH)
                        {
                            output.Add(pScreen);
                        }
                    }
                    else
                    {
                        output.Add(pScreen);
                    }
                }

                last = i;
            }


            // si la cantidad de vertices distintos es 5, tengo que achurar uno
            // para eso busco una secuencia (que en principio siempre existe) de 3 vertices
            // todos por arriba de la pantalla o todos por abajo, etc.
            // En ese caso puedo borrar el punto intermedio sin perder info. 
            // y volver a un quad
            if (output.Count == 6)
            {
                for (int i = 0; i < output.Count; i++)
                {
                    int next = i < 5 ? i + 1 : 0;
                    int next2 = i < 4 ? i + 2 : 1;

                    // estan todos los puntos arriba de la pantalla
                    if (output[i].Y < 0 && output[next].Y < 0 && output[next2].Y < 0)
                    {
                        output.RemoveAt(next);
                        break;
                    }
                    // estan todos los puntos debajo de la pantalla
                    else if (output[i].Y >= height && output[next].Y >= height && output[next2].Y >= height)
                    {
                        output.RemoveAt(next);
                        break;
                    }
                    // estan todos los puntos a izquierda
                    else if (output[i].X < 0 && output[next].X < 0 && output[next2].X < 0)
                    {
                        output.RemoveAt(next);
                        break;
                    }
                    // estan todos los puntos a derecha
                    else if (output[i].X >= width && output[next].X >= width && output[next2].X >= width)
                    {
                        output.RemoveAt(next);
                        break;
                    }
                }
            }
            else if (output.Count == 0)
            {

            }



            return output.ToArray();
        }

        public Vector3 toScreenSpace(Vector4 p, int width, int height)
        {
            //divido por w, (lo paso al proj. space)
            p.X = p.X / p.W;
            p.Y = p.Y / p.W;
            p.Z = p.Z / p.W;

            //lo paso a screen space
            //p.X = (int)((p.X + 1) * 0.5f * width);
            //p.Y = (int)((1 - p.Y) * 0.5f * height);

            p.X = (int)(0.5f + ((p.X + 1) * 0.5f * width));
            p.Y = (int)(0.5f + ((1 - p.Y) * 0.5f * height));

            //p.X = (int)Math.Round((p.X + 1) * 0.5f * width);
            //p.Y = (int)Math.Round((1 - p.Y) * 0.5f * height);


            return new Vector3(p.X, p.Y, p.Z);
        }


        private int clipLine(ref Vector4 p0, ref Vector4 p1, float zn)
        {
            // Verifico si ambos puntos estan antes del near plane
            if (p0.Z < zn && p1.Z < zn)
                // ambos puntos estan detras del near plane, devuelve cero y no hace nada
                return 0;

            if (p0.Z >= zn && p1.Z >= zn)
                // ambos puntos estan delante del near plane, devuelve 2 y no hace nada
                return 2;

            // un pto esta delante y otro detras del near plane
            // busco el punto de inteseccion con el near plane:
            float t = (zn - p0.Z) / (p1.Z - p0.Z);
            Vector4 pZn = new Vector4(
                p0.X + (p1.X - p0.X) * t,
                p0.Y + (p1.Y - p0.Y) * t,
                zn, //p0.Z + (p1.Z - p0.Z) * t,
                1f
                );
            if (p0.Z >= zn)
                // el P0 esta adelante y el P1 detras, la recta clippeada va desde P0 hasta pzn
                // entonces el que cambio es P1
                p1 = pZn;
            else
                p0 = pZn;

            return 1;
        }


        /// <summary>
        /// Proyecta un punto segun el tamaño del DepthBuffer
        /// </summary>
        public Vector3 projectPoint(Vector3 p)
        {
            
            Vector3 pProjected = Vector3.Project(p, this.d3dViewport,
                this.projection, this.view, Matrix.Identity);

            return pProjected;
            

            /*
            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Height;

            //X,Y,Z en wordspace, lo paso a homegenous space
            Matrix m = GuiController.Instance.D3dDevice.Transform.View * this.projection;
            Vector4 pOut = Vector3.Transform(p, m);

            //W positivo
            if (pOut.W > 0)
            {
                //divido por w, (lo paso al proj. space)
                pOut.X = pOut.X / pOut.W;
                pOut.Y = pOut.Y / pOut.W;

                //lo paso a screen space
                pOut.X = (int)((pOut.X + 1) * 0.5f * width);
                pOut.Y = (int)((1 - pOut.Y) * 0.5f * height);
            }

            //W negativo, chequear que no haya situaciones numericas raras
            else
            {

            }




            //divido el Z por w, (lo paso al proj. space)
            pOut.Z = pOut.Z / pOut.W;


            //Devolver punto final proyectado
            return new Vector3(pOut.X, pOut.Y, pOut.Z);
            */
        }

        /// <summary>
        /// Proyecta un punto segun el tamaño del DepthBuffer y le hace clip en Projection space
        /// </summary>
        public Vector3 projectPointAndClip(Vector3 p)
        {

            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Height;

            //X,Y,Z en wordspace, lo paso a homegenous space
            Matrix m = this.view * this.projection;
            Vector4 pOut = Vector3.Transform(p, m);

            //divido por w, (lo paso al proj. space)
            pOut.X = pOut.X / pOut.W;
            pOut.Y = pOut.Y / pOut.W;
            pOut.Z = pOut.Z / pOut.W;

            //lo paso a screen space
            pOut.X = (int)((pOut.X + 1) * 0.5f * width);
            pOut.Y = (int)((1 - pOut.Y) * 0.5f * height);


            //Clamp
            if (pOut.X < 0f) pOut.X = 0f;
            if (pOut.Y < 0f) pOut.Y = 0f;
            if (pOut.Z < 0f) pOut.Z = 0f;
            if (pOut.X >= width) pOut.X = width - 1;
            if (pOut.Y >= height) pOut.Y = height - 1;
            if (pOut.Z > 1f) pOut.Z = 1f;

            //W positivo
            if (pOut.W < 0)
            {

            }


            //Devolver punto final proyectado
            return new Vector3(pOut.X, pOut.Y, pOut.Z);

            

            
        }

        /// <summary>
        /// Proyectar haciendo Clipping
        /// </summary>
        public bool projectQuad_Clipping(Vector3[] p, out Vector3[] screenPoints)
        {
            //Projectar todos los puntos (sin dividir por W aun)
            Matrix m = this.view * this.projection;
            Vector4[] projectedPoints = new Vector4[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                projectedPoints[i] = Vector3.Transform(p[i], m);
            }

            //Hacer clipping del quad en Homogeneous Space
            Vector4[] clippedPoints;
            int numPts = Clipper.ClipPolyToFrustum_SMOOTH(projectedPoints, out clippedPoints);

            //Descartar puntos y lineas
            if (numPts < 3)
            {
                screenPoints = null;
                return false;
            }

            //Convertir los puntos clippeados a Screen Space
            screenPoints = new Vector3[numPts];
            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Height;
            for (int i = 0; i < numPts; i++)
            {
                screenPoints[i] = toScreenSpace(clippedPoints[i], width, height);
            }

            return true;
        }


        








        #region VIEJO


        /*
        /// <summary>
        /// Sutherland-Hodgman
        /// Basado en: http://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman_algorithm
        /// </summary>
        public List<Vector3> clipPolygon2D(Vector3[] p)
        {
            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Width;
            Matrix view = GuiController.Instance.D3dDevice.Transform.View;

            //Transformar a View space
            Vector4[] pView = new Vector4[4];
            for (int i = 0; i < 4; i++)
            {
                pView[i] = Vector3.Transform(p[i], view);
            }



            //Clipping
            List<Vector4> outputList = new List<Vector4>();
            Vector4 s = outputList[outputList.Count - 1];
            for (int i = 0; i < 4; i++)
            {
                Vector4 e = pView[i];
                if (e.Z >= nearPlaneDistance)
                {
                    if (s.Z < nearPlaneDistance)
                    {
                        //Intersect q
                        //outputList.Add(q);
                    }
                    outputList.Add(e);
                }
                else if (s.Z >= nearPlaneDistance)
                {
                    //Intersect q
                    //outputList.Add(q);
                }
                s = e;
            }
        }
        */


        /*
        public Vector3[] projectQuad_3(Vector3[] p)
        {

            int cant_visibles = 0;



            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Width;
            Matrix view = GuiController.Instance.D3dDevice.Transform.View;

            //Transformar a View space
            Vector4[] pView = new Vector4[4];
            for (int i = 0; i < 4; i++)
            {
                pView[i] = Vector3.Transform(p[i], view);
            }

            //Corregir los que estan detras del near plane
            int tripleCase = -1;
            Vector4[] pViewFixed = new Vector4[4];
            for (int i = 0; i < 4; i++)
            {
                bool visible = false;


                if (pView[i].Z < nearPlaneDistance)
                {
                    int prev = i > 0 ? i - 1 : 3;
                    int next = i < 3 ? i + 1 : 0;

                    //Corregir con anterior
                    if (pView[prev].Z >= nearPlaneDistance)
                    {
                        float t = (nearPlaneDistance - pView[i].Z) / (pView[prev].Z - pView[i].Z);
                        Vector4 pZn = new Vector4(
                            pView[i].X + (pView[prev].X - pView[i].X) * t,
                            pView[i].Y + (pView[prev].Y - pView[i].Y) * t,
                            nearPlaneDistance, //pView[i].Z + (pView[prev].Z - pView[i].Z) * t,
                            1f
                            );
                        pViewFixed[i] = pZn;
                    }
                    //Corregir con siguiente
                    else if (pView[next].Z >= nearPlaneDistance)
                    {
                        float t = (nearPlaneDistance - pView[i].Z) / (pView[next].Z - pView[i].Z);
                        Vector4 pZn = new Vector4(
                            pView[i].X + (pView[next].X - pView[i].X) * t,
                            pView[i].Y + (pView[next].Y - pView[i].Y) * t,
                            nearPlaneDistance, //pView[i].Z + (pView[next].Z - pView[i].Z) * t,
                            1f
                            );
                        pViewFixed[i] = pZn;
                    }
                    //Esta en el medio de 2 puntos que estan dentras del near plane, corregir al final de todo
                    else
                    {
                        tripleCase = i;
                    }
                }
                else
                {
                    //ok
                    pViewFixed[i] = pView[i];



                    cant_visibles++;
                    visible = true;


                }

                GuiController.Instance.UserVars["v_" + i] = visible.ToString();


            }

            //Corregir caso triple
            if (tripleCase != -1)
            {
                int prev = tripleCase > 0 ? tripleCase - 1 : 3;
                int next = tripleCase < 3 ? tripleCase + 1 : 0;

                //Generar paralelogramo entre anterior y siguiente
                pViewFixed[tripleCase] = new Vector4(
                    pViewFixed[prev].X + pViewFixed[next].X,
                    pViewFixed[prev].Y + pViewFixed[next].Y,
                    nearPlaneDistance, //pViewFixed[prev].Z + pViewFixed[next].Z,
                    1f
                    );
            }


            //Proyectar todos
            Vector3[] pOut = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector4 pProj = Vector4.Transform(pViewFixed[i], this.projection);
                pOut[i] = toScreenSpace(pProj, width, height);



                GuiController.Instance.UserVars["p_" + i] = pOut[i].X + ", " + pOut[i].Y;
            }




            GuiController.Instance.UserVars["cant_visibles"] = cant_visibles.ToString();


            return pOut;
        }
        */



        /*
        public Vector3[] projectQuad(Vector3[] Q)
        {
            float EPSILON_CERCA_RECTA = 0.1f;
            float EPSILON_MUY_LEJOS_RECTA = 2000f;

            int width = this.d3dViewport.Width;
            int height = this.d3dViewport.Width;

            Matrix m = GuiController.Instance.D3dDevice.Transform.View * this.projection;
            
            Vector4[] pOut = new Vector4[4];
            Vector2[] P = new Vector2[4];
            Vector2[] v = new Vector2[4];
            Vector2[] w = new Vector2[4];
            bool[] visible = new bool[4];
            int cant_visibles = 0;

            for (int i = 0; i < 4; i++)
            {
                pOut[i] = Vector3.Transform(Q[i], m);

                if (pOut[i].W > 0)
                {
                    float X = pOut[i].X / pOut[i].W;
                    float Y = pOut[i].Y / pOut[i].W;
                    float Z = pOut[i].Z / pOut[i].W;

                    if (FastMath.Abs(X) <= 1 && FastMath.Abs(Y) <= 1 && Z >= 0 && Z <= 1)
                    {
                        P[i].X = (X + 1) * 0.5f * width;
                        P[i].Y = (1 - Y) * 0.5f * height;

                        visible[i] = true;
                        cant_visibles++;
                    }

                    // evaluo el pto siguiente
                    {
                        int j = i < 3 ? i + 1 : 0;
                        Vector3 dir = Q[j] - Q[i];
                        dir.Normalize();

                        Vector3 Qs = Q[i] + dir * EPSILON_CERCA_RECTA;
                        Vector4 qOut = Vector3.Transform(Qs, m);

                        X = qOut.X / qOut.W;
                        Y = qOut.Y / qOut.W;
                        Z = qOut.Z / qOut.W;

                        //Pendiente entre el punto actual proyectado y el siguiente proyectado
                        v[i].X = (X + 1) * 0.5f * width;
                        v[i].Y = (1 - Y) * 0.5f * height;
                        v[i] = v[i] - P[i];
                        v[i].Normalize();
                    }

                    // evaluo el pto anterior
                    {
                        int j = i > 0 ? i - 1 : 3;
                        Vector3 dir = Q[j] - Q[i];
                        dir.Normalize();

                        Vector3 Qs = Q[i] + dir * EPSILON_CERCA_RECTA;
                        Vector4 qOut = Vector3.Transform(Qs, m);

                        X = qOut.X / qOut.W;
                        Y = qOut.Y / qOut.W;
                        Z = qOut.Z / qOut.W;

                        //Pendiente entre el punto actual proyectado y el siguiente proyectado
                        w[i].X = (X + 1) * 0.5f * width;
                        w[i].Y = (1 - Y) * 0.5f * height;
                        w[i] = w[i] - P[i];
                        w[i].Normalize();
                    }

                }
            }


            Vector3[] A = new Vector3[4];





            if (cant_visibles > 0)
            {

                if (cant_visibles == 3)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (visible[i])
                        {
                            A[i].X = P[i].X;
                            A[i].Y = P[i].Y;
                        }
                        else
                        {
                            // Lo recupero desde el anterior y desde el siguiente
                            int sig = i < 3 ? i + 1 : 0;
                            int ant = i > 0 ? i - 1 : 3;
                            Vector2 Ip = intersectLine2DLine2D(P[sig], w[sig], P[ant], v[ant]);
                            A[i].X = Ip.X;
                            A[i].Y = Ip.Y;
                        }
                    }
                }
                else if (cant_visibles == 2)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (visible[i])
                        {
                            A[i].X = P[i].X;
                            A[i].Y = P[i].Y;
                        }
                        else
                        {
                            int sig = i < 3 ? i + 1 : 0;
                            int ant = i > 0 ? i - 1 : 3;
                            Vector2 Ip;

                            if (visible[sig] && visible[ant])
                            {
                                // Lo recupero desde el anterior y desde el siguiente
                                Ip = intersectLine2DLine2D(P[sig], w[sig], P[ant], v[ant]);
                                A[i].X = Ip.X;
                                A[i].Y = Ip.Y;
                            }
                            else
                            {
                                // Lo recupero desde el anterior o desde el siguiente
                                if (visible[sig])
                                {
                                    // siguiente
                                    Ip = P[sig] + w[sig] * EPSILON_MUY_LEJOS_RECTA;
                                }
                                else
                                {
                                    // anterior
                                    Ip = P[ant] + v[ant] * EPSILON_MUY_LEJOS_RECTA;
                                }
                                A[i].X = Ip.X;
                                A[i].Y = Ip.Y;
                            }
                        }
                    }
                }
                else if (cant_visibles == 1)
                {
                    // busco el visible
                    int i = 0;
                    while (!visible[i])
                    {
                        ++i;
                    }

                    // Calculo los otros 2 puntos recuperados 
                    Vector2 Qs = P[i] + w[i] * EPSILON_MUY_LEJOS_RECTA;
                    Vector2 Qa = P[i] + v[i] * EPSILON_MUY_LEJOS_RECTA;
                    // Calculo el 3ero en discordia
                    Vector2 T = Qs + Qa - P[i];

                    // Agrego el punto visible
                    A[0].X = P[i].X;
                    A[0].Y = P[i].Y;
                    // Agrego el punto siguiente
                    A[1].X = Qs.X;
                    A[1].Y = Qs.Y;
                    // Agrego el 3ero en disc.
                    A[2].X = T.X;
                    A[2].Y = T.Y;
                    // Agrego el punto anterior
                    A[3].X = Qa.X;
                    A[3].Y = Qa.Y;
                }
                else
                {
                    // todos visibles
                    for (int i = 0; i < 4; i++)
                    {
                        A[i].X = P[i].X;
                        A[i].Y = P[i].Y;
                    }
                }

            }
            else
            {
                //Ver que hacer acá
                //throw new Exception("El quad no tiene ningun punto visible: " + Q);

                //No se ve ninguno, proyectar normalmente
                for (int i = 0; i < 4; i++)
                {
                    float X = pOut[i].X / pOut[i].W;
                    float Y = pOut[i].Y / pOut[i].W;
                    //float Z = pOut[i].Z / pOut[i].W;

                    A[i].X = (X + 1) * 0.5f * width;
                    A[i].Y = (1 - Y) * 0.5f * height;
                }
                
            }


            GuiController.Instance.UserVars["cant_visibles"] = cant_visibles.ToString();


            return A;
        }

        /// <summary>
        /// Encontrar la interseccion entre dos lineas 2D, cada una definida por 
        /// un punto y su pendiente.
        /// </summary>
        private Vector2 intersectLine2DLine2D(Vector2 a, Vector2 dirA, Vector2 b, Vector2 dirB)
        {
            Vector2 a2 = a + dirA;
            Vector2 b2 = b + dirB;

            Vector2 q;
            if (intersectLine2DLine2D(a, a2, b, b2, out q))
            {
                return q;
            }
            throw new Exception("Las lineas no colisionan");
        }

        /// <summary>
        /// Encontrar la interseccion entre dos lineas ab y cd.
        /// Devuelve el punto q de colision, si hay.
        /// Las lineas se consideran infinitas.
        /// </summary>
        private bool intersectLine2DLine2D(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 q)
        {
            //P(t) = A + t1(B - A)
            //P(t) = C + t2(D - C)
            //(igualar)
            //A + t1(B - A) = C + t2(D - C) 

            //(Separar calculo de X e Y)
            //Ax + t1(Bx - Ax) = Cx + t2(Dx - Cx)
            //Ay + t1(By - Ay) = Cy + t2(Dy - Cy)

            //(Resolver sistema lineal por Cramer)
            //t1 = [(Dx - Cx)(Ay - Cy) - (Dy - Cy)(Ax - Cx)] / [(Dy - Cy)(Bx - Ax) - (Dx - Cx)(By - Ay)]
            //t2 = [(Bx - Ax)(Ay - Cy) - (By - Ay)(Ax - Cx)] / [(Dy - Cy)(Bx - Ax) - (Dx - Cx)(By - Ay)] 

            //Calcular denominador
            float denom = (d.Y - c.Y) * (b.X - a.X) - (d.X - c.X) * (b.Y - a.Y);

            //Si es cero, las lineas son paralelas
            if (FastMath.Abs(denom) <= float.Epsilon)
            {
                q = Vector2.Empty;
                return false;
            }

            //Calcular t, solo utilizamos uno de los dos
            float t1 = ((d.X - c.X) * (a.Y - c.Y) - (d.Y - c.Y) * (a.X - c.X)) / denom;
            //float t2 = ((b.X - a.X) * (a.Y - c.Y) - (b.Y - a.Y) * (a.X - c.X)) / denom;
            q = a + t1 * (b - a);
            return true;
        }

*/


        


        #endregion



    }
}
