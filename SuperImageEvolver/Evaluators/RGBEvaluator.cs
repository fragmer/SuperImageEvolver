﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SuperImageEvolver {
    public class RGBEvaluatorFactory : IModuleFactory {
        public Type ModuleType {
            get { return typeof( RGBEvaluator ); }
        }

        public string ID {
            get { return "std.RGBEvaluator.1"; }
        }

        public ModuleFunction Function {
            get { return ModuleFunction.Evaluator; }
        }

        public ModulePreset[] Presets {
            get {
                return new[] {
                    new ModulePreset( "RGB (Fast)", () => ( new RGBEvaluator( false ) ), this ),
                    new ModulePreset( "RGB (Smooth)", () => ( new RGBEvaluator( true ) ), this ),
                };
            }
        }


        public IModule GetInstance() {
            return new RGBEvaluator();
        }
    }


    sealed unsafe class RGBEvaluator : IEvaluator {

        public bool Smooth { get; set; }
        public bool Emphasized { get; set; }
        public double EmphasisAmount { get; set; }

        double maxDivergence;


        public RGBEvaluator() {
            Smooth = true;
            Emphasized = false;
            EmphasisAmount = 2;
        }


        public RGBEvaluator( bool smooth )
            : this() {
            Smooth = smooth;
        }


        public void Initialize( TaskState state ) {}


        public double CalculateDivergence( Bitmap testImage, DNA dna, TaskState state, double maxAcceptableDivergence ) {

            if( Emphasized ) {
                if( EmphasisAmount == 2 ) {
                    maxDivergence = 3L * state.ImageWidth * state.ImageHeight * 255L * 255L;
                } else {
                    maxDivergence = 3L * state.ImageWidth * state.ImageHeight * Math.Pow( 255, EmphasisAmount );
                }
            } else {
                maxDivergence = 3L * state.ImageWidth * state.ImageHeight * 255L;
            }

            double sum = 0;
            double roundedMax = ( maxAcceptableDivergence * maxDivergence + 1 );
            using( Graphics g = Graphics.FromImage( testImage ) ) {
                g.Clear( state.ProjectOptions.Matte );
                g.SmoothingMode = ( Smooth ? SmoothingMode.HighQuality : SmoothingMode.HighSpeed );

                for( int i = 0; i < dna.Shapes.Length; i++ ) {
                    g.FillPolygon( new SolidBrush( dna.Shapes[i].Color ), dna.Shapes[i].Points, FillMode.Alternate );
                }
            }
            byte* originalPointer, testPointer;

            BitmapData testData = testImage.LockBits( new Rectangle( Point.Empty, testImage.Size ),
                                                      ImageLockMode.ReadOnly,
                                                      PixelFormat.Format32bppArgb );

            if( Emphasized ) {
                if( EmphasisAmount == 2 ) {
                    for( int i = 0; i < state.ImageHeight; i++ ) {
                        originalPointer = (byte*)state.WorkingImageData.Scan0 + state.WorkingImageData.Stride * i;
                        testPointer = (byte*)testData.Scan0 + testData.Stride * i;
                        for( int j = 0; j < state.ImageWidth; j++ ) {
                            int b = Math.Abs( *originalPointer - *testPointer );
                            int g = Math.Abs( originalPointer[1] - testPointer[1] );
                            int r = Math.Abs( originalPointer[2] - testPointer[2] );
                            sum += r * r + b * b + g * g;
                            originalPointer += 4;
                            testPointer += 4;
                        }
                        if( sum > roundedMax ) {
                            sum = maxDivergence;
                            break;
                        }
                    }
                } else {
                    for( int i = 0; i < state.ImageHeight; i++ ) {
                        originalPointer = (byte*)state.WorkingImageData.Scan0 + state.WorkingImageData.Stride*i;
                        testPointer = (byte*)testData.Scan0 + testData.Stride*i;
                        for( int j = 0; j < state.ImageWidth; j++ ) {
                            int b = Math.Abs( *originalPointer - *testPointer );
                            int g = Math.Abs( originalPointer[1] - testPointer[1] );
                            int r = Math.Abs( originalPointer[2] - testPointer[2] );
                            sum += Math.Pow( r, EmphasisAmount ) + Math.Pow( g, EmphasisAmount ) +
                                   Math.Pow( b, EmphasisAmount );
                            originalPointer += 4;
                            testPointer += 4;
                        }
                        if( sum > roundedMax ) {
                            sum = maxDivergence;
                            break;
                        }
                    }
                }
            } else {
                for( int i = 0; i < state.ImageHeight; i++ ) {
                    originalPointer = (byte*)state.WorkingImageData.Scan0 + state.WorkingImageData.Stride * i;
                    testPointer = (byte*)testData.Scan0 + testData.Stride * i;
                    for( int j = 0; j < state.ImageWidth; j++ ) {
                        int b = Math.Abs( *originalPointer - *testPointer );
                        int g = Math.Abs( originalPointer[1] - testPointer[1] );
                        int r = Math.Abs( originalPointer[2] - testPointer[2] );
                        sum += r + b + g;
                        originalPointer += 4;
                        testPointer += 4;
                    }
                    if( sum > roundedMax ) {
                        sum = maxDivergence;
                        break;
                    }
                }
            }

            testImage.UnlockBits( testData );
            if( Emphasized ) {
                return Math.Pow( sum / maxDivergence, 1 / EmphasisAmount );
            } else {
                return sum / maxDivergence;
            }
        }


        object ICloneable.Clone() {
            return new RGBEvaluator {
                Smooth = Smooth,
                Emphasized = Emphasized,
                EmphasisAmount = EmphasisAmount
            };
        }


        void IModule.ReadSettings( NBTag tag ) {
            Smooth = tag.GetBool(nameof(Smooth), Smooth);
            Emphasized = tag.GetBool(nameof(Emphasized), Emphasized);
            EmphasisAmount = tag.GetDouble(nameof(EmphasisAmount), EmphasisAmount);
        }

        void IModule.WriteSettings( NBTag tag ) {
            tag.Append(nameof(Smooth), Smooth);
            tag.Append(nameof(Emphasized), Emphasized);
            tag.Append(nameof(EmphasisAmount), EmphasisAmount);
        }
    }
}
