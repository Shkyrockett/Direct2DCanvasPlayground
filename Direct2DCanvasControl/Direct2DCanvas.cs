// <copyright file="DirectXCanvas.cs" company="Shkyrockett" >
// Copyright © 2017 - 2023 Shkyrockett. All rights reserved.
// </copyright>
// <author id="shkyrockett">Shkyrockett</author>
// <license>
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </license>
// <summary></summary>
// <remarks></remarks>

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct2D;
using Windows.Win32.Graphics.Direct2D.Common;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Gdi;

namespace Direct2DCanvasControl;

/// <summary>
/// The Direct2D canvas control.
/// </summary>
/// <seealso cref="System.Windows.Forms.UserControl" />
public partial class Direct2DCanvas
    : UserControl
{
    #region Fields
    /// <summary>
    /// The render target.
    /// </summary>
    private ID2D1DCRenderTarget? dcRenderTarget;

    /// <summary>
    /// Band brush gradient.
    /// </summary>
    private ID2D1LinearGradientBrush? bandBrush;

    /// <summary>
    /// Thread specific random number initializer.
    /// </summary>
    [ThreadStatic]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2019:Improper 'ThreadStatic' field initialization", Justification = "We want random to be different per thread.")]
    private static readonly Random random = new((int)DateTime.Now.Ticks);

    /// <summary>
    /// The size of the gap between bands.
    /// </summary>
    private readonly int gap = 1;

    /// <summary>
    /// The number of bands.
    /// </summary>
    private readonly int bandCount = 10;

    /// <summary>
    /// The frequency.
    /// </summary>
    private readonly double frequency = Math.PI * 2d / 255d;

    /// <summary>
    /// The list of band values.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "The bands are supposed to change at runtime.")]
    private (bool initiated, float value, float t, float start, float goal)[]? bands;

    /// <summary>
    /// The current cycle.
    /// </summary>
    private int cycleCount = 0;

    /// <summary>
    /// The updating
    /// </summary>
    private bool updating;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="Direct2DCanvas" /> class.
    /// </summary>
    public Direct2DCanvas()
    {
        InitializeComponent();
        dcRenderTarget ??= InitializeDirect2DDCRenderTarget<ID2D1Factory7>();
        if (bandBrush is not null) Marshal.ReleaseComObject(bandBrush);
        bandBrush = CreateBandBrush(dcRenderTarget!, ClientSize.Height);

        bands ??= new (bool initiated, float value, float t, float start, float goal)[bandCount];

        // Initialize the world.
        UpdateWorld();

        // Set the number of ticks to refresh the world.
        timer.Interval = 25;//50;

        // Start the world update timer if it isn't in design mode.
        if (!DesignMode) timer.Start();
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets the render target.
    /// </summary>
    /// <value>
    /// The render target.
    /// </value>
    protected ID2D1RenderTarget? RenderTarget => dcRenderTarget;
    #endregion

    #region Events
    /// <summary>
    /// Handles the Tick event of the Timer control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
    private void Timer_Tick(object sender, ElapsedEventArgs e)
    {
        // Process all changes to the world.
        if (!DesignMode) UpdateWorld();

        // Now that the world has been updated it needs to be redrawn.
        Invalidate();
    }

    /// <summary>
    /// Handles the Resize event of the DirectXCanvas control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DirectXCanvas_Resize(object sender, EventArgs e)
    {
        if (dcRenderTarget is not null)
        {
            if (bandBrush is not null) Marshal.ReleaseComObject(bandBrush);
            bandBrush = CreateBandBrush(dcRenderTarget, ClientSize.Height);
        }

        Invalidate();
    }

    /// <summary>
    /// Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event.
    /// </summary>
    /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
    protected unsafe override void OnPaint(PaintEventArgs e)
    {
        dcRenderTarget ??= InitializeDirect2DDCRenderTarget<ID2D1Factory7>();// Recreate resources if they have become invalid.
        dcRenderTarget?.BindDC(e.Graphics, ClientRectangle);
        dcRenderTarget?.BeginDraw();
        RenderScene(dcRenderTarget, bands!, ClientSize, gap, bandBrush!);
        //dcRenderTarget?.EndDraw();
        var result = RenderTarget!.EndDraw(out var tag1, out var tag2);

        if (tag1 != default | tag2 != default)
        {
            Debug.Print($"tag1={tag1}, tag2={tag2}");
        }
        
        if (result == HRESULT.D2DERR_RECREATE_TARGET)
        {
            dcRenderTarget ??= InitializeDirect2DDCRenderTarget<ID2D1Factory7>(); // Recreate resources if they have become invalid.
        }
        Validate();
        base.OnPaint(e);
    }

    /// <summary>
    /// Paints the background of the control.
    /// </summary>
    /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
    protected unsafe override void OnPaintBackground(PaintEventArgs e)
    {
        dcRenderTarget ??= InitializeDirect2DDCRenderTarget<ID2D1Factory7>();// Recreate resources if they have become invalid.
        dcRenderTarget?.BindDC(e.Graphics, ClientRectangle);
        dcRenderTarget?.BeginDraw();
        dcRenderTarget?.Clear(BackColor);
        //dcRenderTarget?.EndDraw();
        var result = RenderTarget!.EndDraw(out var tag1, out var tag2);

        if (tag1 != default | tag2 != default)
        {
            Debug.Print($"tag1={tag1}, tag2={tag2}");
        }

        if (result == HRESULT.D2DERR_RECREATE_TARGET)
        {
            dcRenderTarget ??= InitializeDirect2DDCRenderTarget<ID2D1Factory7>(); // Recreate resources if they have become invalid.
        }
        Validate();
        base.OnPaintBackground(e);
    }
    #endregion

    /// <summary>
    /// Initializes Direct2D for use with a GDI DC.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static unsafe ID2D1DCRenderTarget? InitializeDirect2DDCRenderTarget<T>()
        where T : ID2D1Factory
    {
        var direct2dFactory = PInvoke.D2D1CreateFactory(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED, typeof(T).GUID, new D2D1_FACTORY_OPTIONS(), out var factory) switch { var h when h == HRESULT.S_OK => (T)factory, _ => throw new Exception("Unspecified Error") };
        direct2dFactory.CreateDCRenderTarget(new D2D1_RENDER_TARGET_PROPERTIES(D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT, new D2D1_PIXEL_FORMAT(DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_IGNORE), 0, 0, D2D1_RENDER_TARGET_USAGE.D2D1_RENDER_TARGET_USAGE_GDI_COMPATIBLE, D2D1_FEATURE_LEVEL.D2D1_FEATURE_LEVEL_DEFAULT), out var dCRenderTarget);
        Marshal.ReleaseComObject(direct2dFactory!);
        return dCRenderTarget;
    }

    /// <summary>
    /// Creates the band brush.
    /// </summary>
    /// <param name="renderTarget">The render target.</param>
    /// <param name="height">The height.</param>
    /// <returns></returns>
    private static unsafe ID2D1LinearGradientBrush CreateBandBrush(ID2D1RenderTarget renderTarget, float height)
    {
        var properties = new D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES(new(0, height), new(0, 0));
        ReadOnlySpan<D2D1_GRADIENT_STOP> stops = stackalloc[]
        {
            new D2D1_GRADIENT_STOP(0.0f, Color.Green),
            new D2D1_GRADIENT_STOP(0.85f, Color.Yellow),
            new D2D1_GRADIENT_STOP(1.0f, Color.Red)
        };
        renderTarget.CreateGradientStopCollection(stops, D2D1_GAMMA.D2D1_GAMMA_1_0, D2D1_EXTEND_MODE.D2D1_EXTEND_MODE_CLAMP, out var gradientStops);
        var prop = new D2D1_BRUSH_PROPERTIES(1f, Matrix3x2.Identity);
        renderTarget.CreateLinearGradientBrush(properties, prop, gradientStops, out var bandBrush);
        if (gradientStops is not null) Marshal.ReleaseComObject(gradientStops);
        return bandBrush;
    }

    /// <summary>
    /// Renders the scene.
    /// </summary>
    /// <param name="dcRenderTarget">The dcRenderTarget.</param>
    /// <param name="bands">The bands.</param>
    /// <param name="clientSize">The client size.</param>
    /// <param name="gap">The gap.</param>
    /// <param name="brush">The brush.</param>
    /// <returns></returns>
    private static unsafe void RenderScene(ID2D1DCRenderTarget? dcRenderTarget, (bool initiated, float value, float t, float start, float goal)[] bands, Size clientSize, int gap, ID2D1Brush brush)
    {
        if (dcRenderTarget is null) return;
        /*var pSize =*/
        dcRenderTarget.GetPixelSize(out var pSize);
        /*var size =*/
        dcRenderTarget.GetSize(out var size);
        _ = size;
        var dimensions = pSize/*.ToSize()*/; //clientSize;
        _ = clientSize;

        var bandCount = bands?.Length ?? 0;
        var bandWidth = ((int)dimensions.width + gap) / bandCount;
        var i = 0;

        if (bands is not null)
        {
            foreach (var (initiated, value, t, start, goal) in bands)
            {
                int left = (i * bandWidth);
                float top = (float)dimensions.height * value / 100f;
                int width = bandWidth - gap;
                float height = dimensions.height - dimensions.height * value / 100f;
                var rectangle = new RectangleF(left, top, width, height);
                dcRenderTarget?.FillRectangle((D2D_RECT_F)rectangle, brush!);
                i++;
            }
        }
    }

    /// <summary>
    /// Updates the world.
    /// </summary>
    /// <returns></returns>
    private void UpdateWorld()
    {
        if (updating) return;
        updating = true;
        var phase = 0;
        var center = 100; // 200; // 128;
        var width = 55; // 127;

        if (cycleCount > 0xff)
        {
            cycleCount = 0;
        }

        cycleCount++;

        BackColor = Color.FromArgb(0xff,
            (int)Math.Floor(Math.Sin(frequency * cycleCount + 0 + phase) * width + center),
            (int)Math.Floor(Math.Sin(frequency * cycleCount + 2 + phase) * width + center),
            (int)Math.Floor(Math.Sin(frequency * cycleCount + 4 + phase) * width + center));

        if (bands is not null)
            for (var i = 0; i < bandCount; i++)
            {
                if (!bands[i].initiated)
                {
                    var s = random?.Next(1, 99) ?? 1;
                    var g = random?.Next(1, 99) ?? 99;
                    bands[i] = new(true, s, 0, s, g);
                }
                else
                {
                    if (float.IsNaN(bands[i].start)) bands[i].start = random?.Next(1, 99) ?? 1;
                    var distance = (int)Distance(bands[i].start, bands[i].goal);
                    bands[i].value = Linear(bands[i].t, bands[i].start, bands[i].goal, distance);
                    int sign = Math.Sign(bands[i].goal - bands[i].start);
                    bands[i].t += 1 * sign;
                    if ((bands[i].value == 0 && bands[i].goal == 0)
                        || (sign == 1 && bands[i].value >= bands[i].goal)
                        || (sign == -1 && bands[i].value <= bands[i].goal)
                        || (sign == 0))
                    {
                        bands[i].t = 0;
                        bands[i].start = bands[i].value;
                        bands[i].goal = random?.Next(1, 99) ?? 1;
                    }
                }
            }

        updating = false;
    }

    /// <summary>
    /// The linear interpolation method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t">The t.</param>
    /// <param name="u0">The u0.</param>
    /// <param name="u1">The u1.</param>
    /// <returns>
    /// The linear interpolation method.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Lerp<T>(T t, T u0, T u1) where T : IFloatingPointIeee754<T> => u0 + ((u1 - u0) * t);

    /// <summary>
    /// Easing equation function for a simple linear tweening, with no easing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t">Current time elapsed in ticks.</param>
    /// <param name="b">Starting value.</param>
    /// <param name="c">Final value.</param>
    /// <param name="d">Duration of animation.</param>
    /// <returns>
    /// The correct value.
    /// </returns>
    /// <acknowledgment>
    /// https://github.com/darrendavid/wpf-animation
    /// </acknowledgment>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Linear<T>(T t, T b, T c, T d) where T : IFloatingPointIeee754<T> => b + (c * t / d);

    /// <summary>
    /// Calculates the distance between two points in 2-dimensional Euclidean space.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="a">First component.</param>
    /// <param name="b">Second component.</param>
    /// <returns>
    /// Returns the distance between two points.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Distance<T>(T a, T b) where T : IFloatingPointIeee754<T> => T.Abs(b - a);
}
