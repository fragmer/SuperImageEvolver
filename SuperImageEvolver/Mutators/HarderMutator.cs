﻿using System;
using System.Drawing;
using System.IO;


namespace SuperImageEvolver {

    public class HarderMutatorFactory : IModuleFactory {
        public Type ModuleType { get { return typeof( HarderMutator ); } }
        public string ID { get { return "std.HarderMutator.1"; } }
        public ModuleFunction Function { get { return ModuleFunction.Mutator; } }
        public ModulePreset[] Presets {
            get {
                return new ModulePreset[]{
                    new ModulePreset("Harder", ()=>new HarderMutator(), this )
                };
            }
        }
        public IModule GetInstance() { return new HarderMutator(); }
    }


    class HarderMutator : IMutator {
        public int MaxOverlap { get; set; }
        public double MaxPolygonArea { get; set; }

        public HarderMutator() {
            MaxOverlap = 6;
            MaxPolygonArea = .5;
        }

        public DNA Mutate( Random rand, DNA oldDNA, TaskState task ) {
            DNA newDNA = new DNA( oldDNA );
            Shape shape = newDNA.Shapes[rand.Next( newDNA.Shapes.Length )];
            shape.PreviousState = shape.Clone() as Shape;
            shape.Color = Color.FromArgb( rand.Next( 1, 256 ), rand.Next( 256 ), rand.Next( 256 ), rand.Next( 256 ) );
            double area, maxArea = MaxPolygonArea * task.ImageWidth * task.ImageHeight;
            do {
                for( int i = 0; i < shape.Points.Length; i++ ) {
                    shape.Points[i] = new PointF( rand.NextFloat( -MaxOverlap, task.ImageWidth + MaxOverlap ),
                                                  rand.NextFloat( -MaxOverlap, task.ImageHeight + MaxOverlap ) );
                }
                area = CalculateArea( shape.Points );
            } while( area > maxArea );
            newDNA.LastMutation = MutationType.ReplaceShape;
            return newDNA;
        }

        static double CalculateArea( PointF[] points ) {
            float minX = float.MaxValue, maxX = float.MinValue,
                  minY = float.MaxValue, maxY = float.MinValue;
            for( int i = 0; i < points.Length; i++ ) {
                minX = Math.Min( minX, points[i].X );
                maxX = Math.Max( maxX, points[i].X );
                minY = Math.Min( minY, points[i].Y );
                maxY = Math.Max( maxY, points[i].Y );
            }
            return (maxX - minX) * (maxY - minY);
        }

        object ICloneable.Clone() {
            return new HarderMutator();
        }


        void IModule.ReadSettings( NBTag tag ) { }

        void IModule.WriteSettings( NBTag tag ) { }
    }
}