//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ViewDeckController.cs" company="sgmunn">
//    (c) sgmunn 2012  
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//    documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//    to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
//    the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//    THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//    IN THE SOFTWARE.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

// Derived from https://github.com/Inferis/ViewDeck
// Modified a little to support iOS 5+ only

namespace MonoKit.UI.ViewDeck
{
    using System;
    using System.Collections.Generic;    
    using Foundation;
    using UIKit;
    using System.Diagnostics;
    using CoreGraphics;
    using ObjCRuntime;

    public enum ViewDeckPanningMode
    {
        NoPanning,
        FullViewPanning,
        NavigationBarPanning,
        PanningViewPanning
    }

    public enum CenterHiddenInteractivity
    {
        UserInteractive,
        NotUserInteractive,
        NotUserInteractiveWithTapToClose,
        NotUserInteractiveWithTapToCloseBouncing
    }

    public enum ViewDeckNavigationControllerBehavior
    {
        Contained,
        Integrated
    }

    public enum ViewDeckRotationBehavior
    {
        KeepsLedgeSizes,
        // I don't know if this is implemented or not
        KeepsViewSizes
    }

    /// <summary>
    /// View deck controller.
    /// </summary>
    /// <remarks>
    /// It is sealed because it uses weakdelegates which don't work in sub-classes
    /// </remarks>
    public sealed class ViewDeckController : UIViewController
    {
        #region Private Fields

        private readonly List<UIGestureRecognizer> panners;

        private CenterHiddenInteractivity centerHiddenInteractivity;
        private bool viewAppeared;

        private UIViewController slidingController;

        private nfloat originalShadowRadius;
        private CGSize originalShadowOffset;
        private UIColor originalShadowColor;
        private nfloat originalShadowOpacity;

        private UIView referenceView;
        private UIBezierPath originalShadowPath;
        private UIView centerView;
        private UIButton centerTapper;

        private nfloat offset;
        private nfloat preRotationWidth;
        private nfloat preRotationCenterWidth;
        private nfloat panOrigin;

		private CGSize preRotationSize;
		//private SizeF preRotationCenterSize;
		//private bool preRotationIsLandscape;
		//UIInterfaceOrientation willAppearShouldArrangeViewsAfterRotation;

        private UIViewController _centerController;
        private UIViewController _leftController;
        private UIViewController _rightController;
        private nfloat _rightLedge;
        private nfloat _leftLedge;
        private ViewDeckNavigationControllerBehavior _navigationControllerBehavior;
        private ViewDeckPanningMode _panningMode;
        private UIView _panningView;
        private nfloat _maxLedge;
        private bool _automaticallyUpdateTabBarItems;

        #endregion

        #region Constructors

        public ViewDeckController(UIViewController centerController)
        {
            this.panners = new List<UIGestureRecognizer>();
            this.Enabled = true;
            this.Elastic = true;

// ??             this.originalShadowColor = UIColor.Clear;

            this.RotationBehavior = ViewDeckRotationBehavior.KeepsLedgeSizes;

            this.PanningMode = ViewDeckPanningMode.FullViewPanning;
            this.centerHiddenInteractivity = CenterHiddenInteractivity.UserInteractive;

            this.LeftLedge = 44;
            this.RightLedge = 44;
        
            this.CenterController = centerController;
        }


        public ViewDeckController(UIViewController centerController, UIViewController leftController) : this(centerController)
        {
            this.LeftController = leftController;
        }

        public ViewDeckController(UIViewController centerController, UIViewController leftController, UIViewController rightController) : this(centerController)
        {
            this.LeftController = leftController;
            this.RightController = rightController;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public UIViewController CenterController
        {
            get
            {
                return this._centerController;
            }

            set
            {
                this.SetCenterController(value);
            }
        }

        /// <summary>
        /// </summary>
        public UIViewController LeftController
        {
            get
            {
                return this._leftController;
            }

            set
            {
                if (this.LeftController == value) 
                {
                    return;
                }

                this.ApplySideController(ref this._leftController, value, this.RightController, () => { this.RightController = null; });
            }
        }

        /// <summary>
        /// </summary>
        public UIViewController RightController
        {
            get
            {
                return this._rightController;
            }

            set
            {
                if (this.RightController == value) 
                {
                    return;
                }

                this.ApplySideController(ref this._rightController, value, this.LeftController, () => { this.LeftController = null; });
            }
        }
        
        public override string Title
        {
            get
            {
                return base.Title;
            }

            set
            {
                if (base.Title != value)
                {
                    base.Title = value;
                    this.CenterController.Title = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the view deck is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the deck can be stretched past the ledges.
        /// </summary>
        public bool Elastic { get; set; }

        /// <summary>
        /// </summary>
        public nfloat RightLedge
        {
            get
            {
                return this._rightLedge;
            }

            set
            {
                this.SetRightLedge(value);
            }
        }

        /// <summary>
        /// </summary>
        public nfloat LeftLedge
        {
            get
            {
                return this._leftLedge;
            }

            set
            {
                this.SetLeftLedge(value);
            }
        }
        
        /// <summary>
        /// </summary>
        public nfloat MaxLedge
        {
            get
            {
                return this._maxLedge;
            }

            set
            {
                this._maxLedge = value;

                if (this.LeftController != null && this.RightController != null) 
                {
                    Console.WriteLine("ViewDeckController: warning: setting maxLedge with 2 side controllers. Value will be ignored.");
                    return;
                }
                
                if (this.LeftController != null && this.LeftLedge > this.MaxLedge) 
                {
                    this.LeftLedge = value;
                }
                else if (this.RightController != null && this.RightLedge > this.MaxLedge) 
                {
                    this.RightLedge = value;
                }
                
                this.SetSlidingFrameForOffset(this.offset);
            }
        }

        public bool ResizesCenterView { get; set; }

        public CenterHiddenInteractivity CenterInteractivity
        {
            get
            {
                return this.centerHiddenInteractivity;
            }

            set
            {
                this.centerHiddenInteractivity = value;
            }
        }

        /// <summary>
        /// </summary>
        public ViewDeckNavigationControllerBehavior NavigationControllerBehavior
        {
            get
            {
                return this._navigationControllerBehavior;
            }

            set
            {
                if (this.viewAppeared) 
                {
                    throw new InvalidOperationException("Cannot set navigationcontroller behavior when the view deck is already showing.");
                }

                this._navigationControllerBehavior = value;
            }
        }

        /// <summary>
        /// </summary>
        public ViewDeckPanningMode PanningMode
        {
            get
            {
                return this._panningMode;
            }

            set
            {
                if (this.viewAppeared) 
                {
                    this.RemovePanners();
                    this._panningMode = value;
                    this.AddPanners();
                }
                else
                {
                    this._panningMode = value;
                }
            }
        }

        /// <summary>
        /// </summary>
        public UIView PanningView
        {
            get
            {
                return this._panningView;
            }

            set
            {
                if (this._panningView != value) 
                {
                    // todo: dispose _panningView ??
                    //II_RELEASE(_panningView);
                    this._panningView = value;
                    
                    if (this.viewAppeared && this.PanningMode == ViewDeckPanningMode.PanningViewPanning)
                    {
                        this.AddPanners();
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        public ViewDeckRotationBehavior RotationBehavior { get; set; }

        /// <summary>
        /// </summary>
        public bool AutomaticallyUpdateTabBarItems
        {
            get
            {
                return this._automaticallyUpdateTabBarItems;
            }

            set
            {
                if (this._automaticallyUpdateTabBarItems) 
                {
                    this.TryRemoveObserver(this.CenterController, new NSString("tabBarItem.title"));
                    this.TryRemoveObserver(this.CenterController, new NSString("tabBarItem.image"));
                    this.TryRemoveObserver(this.CenterController, new NSString("hidesBottomBarWhenPushed"));
                }
                
                this._automaticallyUpdateTabBarItems = value;

                if (value) 
                {
                    this.CenterController.AddObserver(this, new NSString("tabBarItem.title"), 0, IntPtr.Zero);
                    this.CenterController.AddObserver(this, new NSString("tabBarItem.image"), 0, IntPtr.Zero);
                    this.CenterController.AddObserver(this, new NSString("hidesBottomBarWhenPushed"), 0, IntPtr.Zero);

                    if (this.CenterController.TabBarItem.Title != null)
                    {
                        this.TabBarItem.Title = this.CenterController.TabBarItem.Title;
                    }

                    if (this.CenterController.TabBarItem.Image != null)
                    {
                        this.TabBarItem.Image = this.CenterController.TabBarItem.Image;
                    }
                }
            }
        }
                
        /// <summary>
        /// </summary>
        public bool LeftControllerIsClosed 
        {
            get
            {
                return this.LeftController == null || this.SlidingControllerView.Frame.GetMinX() <= 0;
            }
        }

        /// <summary>
        /// </summary>
        public bool RightControllerIsClosed 
        {
            get
            {
                return this.RightController == null || this.SlidingControllerView.Frame.GetMaxX() >= this.ReferenceBounds.Size.Width;
            }
        }

        /// <summary>
        /// </summary>
        public bool LeftControllerIsOpen 
        {
            get
            {
                return this.LeftController != null && this.SlidingControllerView.Frame.GetMinX() < this.ReferenceBounds.Size.Width 
                    && this.SlidingControllerView.Frame.GetMinX() >= this.RightLedge;
            }
        }

        /// <summary>
        /// </summary>
        public bool RightControllerIsOpen 
        {
            get
            {
                return this.RightController != null && this.SlidingControllerView.Frame.GetMaxX() < this.ReferenceBounds.Size.Width 
                    && this.SlidingControllerView.Frame.GetMaxX() >= this.LeftLedge;
            }
        }

        public ViewDeckControllerDelegate Delegate
        {
            get;
            set;
        }

        #endregion

        #region Private Properties

        /// <summary>
        /// </summary>
        private UIView SlidingControllerView 
        {
            get
            {
                if (this.NavigationController != null && this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Integrated) 
                {
                    return this.slidingController.View;
                }
                else 
                {
                    return this.centerView;
                }
            }
        }

        private CGRect ReferenceBounds
        {
            get
            {
                if (this.referenceView != null)
                {
                    return this.referenceView.Bounds;
                }

                return CGRect.Empty;
            }
        }

        private nfloat RelativeStatusBarHeight
        {
            get
            {
                if (this.referenceView != null && !this.referenceView.GetType().IsSubclassOf(typeof(UIWindow)))
                {
                    return 0;
                }   

                return this.StatusBarHeight;
            }
        }

        private nfloat StatusBarHeight 
        {
            get
            {
                switch (UIApplication.SharedApplication.StatusBarOrientation)
                {
                    case UIInterfaceOrientation.LandscapeLeft:
                    case UIInterfaceOrientation.LandscapeRight:
                        return UIApplication.SharedApplication.StatusBarFrame.Width;
                    default:
                        return UIApplication.SharedApplication.StatusBarFrame.Height;
                }
            }
        }

        private static CGRect RectangleShrink(CGRect rect, nfloat width, nfloat height)
        {
            return new CGRect(rect.X, rect.Y, rect.Width - width, rect.Height - height);
        }

        private CGRect CenterViewBounds 
        {
            get
            {
                if (this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Contained)
                {
                    return this.ReferenceBounds;
                }

                nfloat height = 0f;
                if (this.NavigationController != null)
                {
                    height = this.NavigationController.NavigationBarHidden ? 0 : this.NavigationController.NavigationBar.Frame.Size.Height;
                }

                return RectangleShrink(this.ReferenceBounds, 0, this.RelativeStatusBarHeight + height);
            }
        }

        private CGRect SideViewBounds 
        {
            get
            {
                if (this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Contained)
                    return this.ReferenceBounds;
            
                return RectangleOffsetTopAndShrink(this.ReferenceBounds, this.RelativeStatusBarHeight);
            }
        }

        #endregion

        #region Public Methods

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();

            this.CenterController.DidReceiveMemoryWarning();

            if (this.LeftController != null)
            {
                this.LeftController.DidReceiveMemoryWarning();
            }

            if (this.RightController != null)
            {
                this.RightController.DidReceiveMemoryWarning();
            }
        }

        public override void LoadView()
        {
            this.offset = 0;
            this.viewAppeared = false;

            this.View = new UIView();
            this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.View.AutosizesSubviews = true;
            this.View.ClipsToBounds = true;
        }

        public override void ViewDidLoad() 
        {
            base.ViewDidLoad();
            
            this.centerView = new UIView();
            this.centerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.centerView.AutosizesSubviews = true;
            this.centerView.ClipsToBounds = true;
            this.View.AddSubview(this.centerView);
            
            this.originalShadowRadius = 0;
            this.originalShadowOpacity = 0;
            this.originalShadowColor = null;
            this.originalShadowOffset = CGSize.Empty;
            this.originalShadowPath = null;
        }

        [Obsolete]
        public override void ViewDidUnload()
        {
            this.CleanUp();

            base.ViewDidUnload();
        }

        public override void ViewWillAppear(bool animated) 
        {
            base.ViewWillAppear(animated);
            
            this.View.AddObserver(this, new NSString("bounds"),  NSKeyValueObservingOptions.New, IntPtr.Zero);
            if (this.viewAppeared)
            {
            this.CenterController.AddObserver(this, new NSString("title"), 0, IntPtr.Zero);
            }

            Action applyViews = () => 
            {        
                this.CenterController.View.RemoveFromSuperview();
                this.centerView.AddSubview(this.CenterController.View);

                if (this.LeftController != null)
                {
                    this.LeftController.View.RemoveFromSuperview();
                    this.referenceView.InsertSubviewBelow(this.LeftController.View, this.SlidingControllerView);
                }

                if (this.RightController != null)
                {
                    this.RightController.View.RemoveFromSuperview();
                    this.referenceView.InsertSubviewBelow(this.RightController.View, this.SlidingControllerView);
                }

                this.ReapplySideController(this.LeftController);
                this.ReapplySideController(this.RightController);
                
                this.SetSlidingFrameForOffset(this.offset);
                this.SlidingControllerView.Hidden = false;
                
                this.centerView.Frame = this.CenterViewBounds;
                this.CenterController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                this.CenterController.View.Frame = this.centerView.Bounds;

                if (this.LeftController != null)
                {
                    this.LeftController.View.Frame = this.SideViewBounds;
                    this.LeftController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                }

                if (this.RightController != null)
                {
                    this.RightController.View.Frame = this.SideViewBounds;
                    this.RightController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                }

                this.ApplyShadowToSlidingView();
            };

            if (this.SetSlidingAndReferenceViews()) 
            {
                applyViews();
            }

            this.viewAppeared = true;

            this.PerformSelector(() => 
                {
                    if (this.referenceView != null) 
                    {
                        this.SetSlidingAndReferenceViews();
                        applyViews();
                    }

                    this.SetSlidingFrameForOffset(this.offset);
                    this.HideAppropriateSideViews();
                }, 0.01f);
            
            this.AddPanners();
            
            if (this.SlidingControllerView.Frame.Location.X == 0.0f) 
            {
                this.CenterViewVisible();
            }
            else
            {
                this.CenterViewHidden();
            }
        }

        public override void ViewDidAppear(bool animated) 
        {
            base.ViewDidAppear(animated);
        }

        public override void ViewWillDisappear(bool animated) 
        {
            base.ViewWillDisappear(animated);
  
            this.RemovePanners();
        }

        public override void ViewDidDisappear(bool animated) 
        {
            base.ViewDidDisappear(animated);

            this.TryRemoveObserver(this.View, "bounds");
            this.TryRemoveObserver(this.CenterController, "title");

            if (this.AutomaticallyUpdateTabBarItems)
            {
                this.TryRemoveObserver(this.CenterController, "tabBarItem.title");
                this.TryRemoveObserver(this.CenterController, "tabBarItem.image");
                this.TryRemoveObserver(this.CenterController, "hidesBottomBarWhenPushed");
            }
        }

		private bool IsLandscape(UIInterfaceOrientation orientation)
		{
			return orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight;
		}

        [Obsolete]
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
			this.preRotationSize = this.ReferenceBounds.Size;
			//this.preRotationCenterSize = this.CenterViewBounds.Size;
			//this.preRotationIsLandscape = IsLandscape (UIApplication.SharedApplication.StatusBarOrientation);				
			//this.willAppearShouldArrangeViewsAfterRotation = toInterfaceOrientation;

            this.preRotationWidth = this.ReferenceBounds.Size.Width;
            this.preRotationCenterWidth = this.CenterViewBounds.Size.Width;//was - this.centerView.Bounds.Size.Width;

            bool should = true;
            if (this.CenterController != null)
            {
                should = this.CenterController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }

            return should;
        }

		public override bool ShouldAutorotate()
		{
			bool should = true;
			if (this.CenterController != null)
			{
				should = this.CenterController.ShouldAutorotate();
			}
			return should;
		}

		public override UIInterfaceOrientation PreferredInterfaceOrientationForPresentation()
		{
			var or = UIInterfaceOrientation.Portrait;
			if (this.CenterController != null)
			{
				or = this.CenterController.PreferredInterfaceOrientationForPresentation();
			}
			return or;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
		{
			var or = UIInterfaceOrientationMask.All;
			if (this.CenterController != null)
			{
				or = this.CenterController.GetSupportedInterfaceOrientations();
			}
			return or;
		}

        public override void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillAnimateRotation(toInterfaceOrientation, duration);
            
            this.ArrangeViewsAfterRotation();
        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillRotate(toInterfaceOrientation, duration);
            this.RestoreShadowToSlidingView();

			if (preRotationSize.Width == 0) 
			{
				this.preRotationSize = this.ReferenceBounds.Size;
				//this.preRotationCenterSize = this.CenterViewBounds.Size;
				//this.preRotationIsLandscape = IsLandscape (UIApplication.SharedApplication.StatusBarOrientation);				

				this.preRotationWidth = this.ReferenceBounds.Size.Width;
				this.preRotationCenterWidth = this.CenterViewBounds.Size.Width;//was - this.centerView.Bounds.Size.Width;
			}
        }

        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate(fromInterfaceOrientation);
            this.ApplyShadowToSlidingView();
        }
        
        public override void ObserveValue(NSString keyPath, NSObject @object, NSDictionary change, IntPtr context)
        {
            if (@object == this.CenterController) 
            {
                if (keyPath.Equals(new NSString("tabBarItem.title"))) 
                {
                    this.TabBarItem.Title = this.CenterController.TabBarItem.Title;
                    return;
                }
                
                if (keyPath.Equals(new NSString("tabBarItem.image"))) 
                {
                    this.TabBarItem.Image = this.CenterController.TabBarItem.Image;
                    return;
                }

                if (keyPath.Equals(new NSString("hidesBottomBarWhenPushed"))) 
                {
                    this.HidesBottomBarWhenPushed = this.CenterController.HidesBottomBarWhenPushed;
                    this.TabBarController.HidesBottomBarWhenPushed = this.CenterController.HidesBottomBarWhenPushed;
                    return;
                }
            }

            if (keyPath.Equals(new NSString("title"))) 
            {
                if (this.Title != this.CenterController.Title) 
                {
                    this.Title = this.CenterController.Title ?? string.Empty;
                }
                return;
            }
            
            if (keyPath.Equals(new NSString("bounds"))) 
            {
                var offset = this.SlidingControllerView.Frame.Location.X;
                this.SetSlidingFrameForOffset(offset);

                this.SlidingControllerView.Layer.ShadowPath = UIBezierPath.FromRect(this.ReferenceBounds).CGPath;
                UINavigationController navController = this.CenterController.GetType().IsSubclassOf(typeof(UINavigationController)) ? (UINavigationController)this.CenterController : null;

                if (navController != null && !navController.NavigationBarHidden) 
                {
                    navController.NavigationBarHidden = true;
                    navController.NavigationBarHidden = false;
                }

                return;
            }
        }

        public IEnumerable<UIViewController> Controllers()
        {
            yield return this.CenterController;

            if (this.LeftController != null)
            {
                yield return this.LeftController;
            }

            if (this.RightController != null)
            {
                yield return this.RightController;
            }
        }

        public void ShowCenterView() 
        {
            this.ShowCenterView(true);
        }

        public void ShowCenterView(bool animated) 
        {
            this.ShowCenterView(animated, null);
        }

        public bool ToggleLeftView() 
        {
            return this.ToggleLeftView(true);
        }

        public bool OpenLeftView() 
        {
            return this.OpenLeftView(true);
        }

        public bool CloseLeftView()
        {
            return this.CloseLeftView(true);
        }

        public bool ToggleLeftView(bool animated)
        {
            return this.ToggleLeftView(animated, null);
        }

        public bool ToggleLeftView(bool animated, Action<ViewDeckController> completed)
        {
            if (this.LeftControllerIsClosed) 
            {
                return this.OpenLeftView(animated, completed);
            }
            else
            {
                return this.CloseLeftView(animated, completed);
            }
        }

        public bool OpenLeftView(bool animated) 
        {
            return this.OpenLeftView(animated, null);
        }

        public bool OpenLeftView(bool animated, Action<ViewDeckController> completed)
        {
            return this.OpenLeftView(animated, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }
        
        public bool OpenLeftViewBouncing(Action<ViewDeckController> bounced)
        {
            return this.OpenLeftViewBouncing(bounced, null);
        }

        public bool OpenLeftViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController>completed) 
        {
            return this.OpenLeftViewBouncing(bounced, true, completed);
        }
        
        public bool CloseLeftView(bool animated) 
        {
            return this.CloseLeftView(animated, null);
        }

        public bool CloseLeftView(bool animated, Action<ViewDeckController> completed)
        {
            return this.CloseLeftView(animated,true, completed);
        }
        
        public bool CloseLeftViewBouncing(Action<ViewDeckController> bounced) 
        {
            return this.CloseLeftViewBouncing(bounced, null);
        }

        public bool CloseLeftViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController> completed) 
        {
            return this.CloseLeftViewBouncing(bounced, true, completed);
        }
        
        public bool ToggleRightView() 
        {
            return this.ToggleRightView(true);
        }

        public bool OpenRightView() 
        {
            return this.OpenRightView(true);
        }

        public bool CloseRightView() 
        {
            return this.CloseRightView(true);
        }

        public bool ToggleRightView(bool animated)
        {
            return this.ToggleRightView(animated, null);
        }

        public bool ToggleRightView(bool animated, Action<ViewDeckController> completed) 
        {
            if (this.RightControllerIsClosed) 
            {
                return this.OpenRightView(animated, completed);
            }
            else
            {
                return this.CloseRightView(animated, completed);
            }
        }

        public bool OpenRightView(bool animated)
        {
            return this.OpenRightView(animated, null);
        }

        public bool OpenRightView(bool animated, Action<ViewDeckController> completed) 
        {
            return this.OpenRightView(animated, UIViewAnimationOptions.CurveEaseInOut,true, completed);
        }
        
        public bool CloseRightViewBouncing(Action<ViewDeckController> bounced) 
        {
            return this.CloseRightViewBouncing(bounced, null);
        }

        public bool CloseRightViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController> completed) 
        {
            return this.CloseRightViewBouncing(bounced, true, completed);
        }
        
        public bool OpenRightViewBouncing(Action<ViewDeckController> bounced) 
        {
            return this.OpenRightViewBouncing(bounced, null);
        }

        public bool OpenRightViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController> completed) 
        {
            return this.OpenRightViewBouncing(bounced, true, completed);
        }
        
        public bool CloseRightView(bool animated)
        {
            return this.CloseRightView(animated, null);
        }

        public bool CloseRightView(bool animated, Action<ViewDeckController> completed)
        {
            return this.CloseRightView(animated, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }
        
        public void RightViewPushViewControllerOverCenterController(UIViewController controller) 
        {
            Debug.Assert(this.CenterController.GetType().IsSubclassOf(typeof(UINavigationController)), "cannot rightViewPushViewControllerOverCenterView when center controller is not a navigation controller");

            UIGraphics.BeginImageContextWithOptions(this.View.Bounds.Size, true, 0);

            CGContext context = UIGraphics.GetCurrentContext();
            this.View.Layer.RenderInContext(context);

            UIImage deckshot = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            
            UIImageView shotView = new UIImageView(deckshot);
            shotView.Frame = this.View.Frame; 
            this.View.Superview.AddSubview(shotView);

            CGRect targetFrame = this.View.Frame; 

            this.View.Frame = RectangleFOffset(this.View.Frame, this.View.Frame.Size.Width, 0);
            
            this.CloseRightView(true);

            UINavigationController navController = ((UINavigationController)this.CenterController);
            navController.PushViewController(controller, false);
            
            UIView.Animate(0.3, 0, UIViewAnimationOptions.TransitionNone, () =>
           {
                shotView.Frame = RectangleFOffset(shotView.Frame, -this.View.Frame.Size.Width, 0);
                this.View.Frame = targetFrame;
            },
                () => 
                {
                shotView.RemoveFromSuperview();
            });
        }

        #endregion
          
        #region Private Statics

        private static bool FloatEqual(nfloat a, nfloat b)
        {
            return (a - b == 0);	// still bad either way
        }
        
        private static float SlideDuration(bool animated, float duration)
        {
            return animated ? duration : 0;
        }

        private static float CloseSlideDuration(bool animated)
        {
            return SlideDuration(animated, 0.3f);
        }

        private static float OpenSlideDuration(bool animated)
        {
            return SlideDuration(animated, 0.3f);
        }
        
        private static CGRect RectangleOffsetTopAndShrink(CGRect rect, nfloat offset)
        {
            return new CGRect(rect.X, rect.Y + offset, rect.Width, rect.Height - offset);
        }

        #endregion
        
        private void CleanUp()
        {
            this.originalShadowRadius = 0;
            this.originalShadowOpacity = 0;
            this.originalShadowColor = null;
            this.originalShadowOffset = CGSize.Empty;
            this.originalShadowPath = null;
            
            this.slidingController = null;
            this.referenceView = null;
            this.centerView = null;
            this.centerTapper = null;
        }

        // todo: test when we need to call 'Dealloc' properly
        private void Dealloc()
        {
            this.CleanUp();
            
//            this.centerController.viewDeckController = null;
            this.CenterController = null;

            if (this.LeftController != null)
            {
//                this.leftController.viewDeckController = null;
                this.LeftController = null;
            }

            if (this.RightController != null)
            {
//                this.rightController.viewDeckController = null;
                this.RightController = null;
            }

            this.panners.Clear();
        }
        
        private void PerformSelector(Action action, nfloat delay)
        {
            int d = (int)(1000 * delay);
            
            var thread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                System.Threading.Thread.Sleep(d);
                this.InvokeOnMainThread(action);
                action = null;
            }));
            
            thread.IsBackground = true;
            thread.Start();
        }

        private nfloat LimitOffset(nfloat offset) 
        {
            if (this.LeftController != null && this.RightController != null) 
            {
                return offset;
            }

            if (this.LeftController != null && this.MaxLedge > 0) 
            {
                var left = this.ReferenceBounds.Size.Width - this.MaxLedge;
				offset = (nfloat)Math.Max(offset, left);
            }
            else if (this.RightController != null && this.MaxLedge > 0) 
            {
                var right = this.MaxLedge - this.ReferenceBounds.Size.Width;
				offset = (nfloat)Math.Min(offset, right);
            }
            
            return offset;
        }

        private CGRect SlidingRectForOffset(nfloat offset) 
        {
            offset = this.LimitOffset(offset);

            var sz = this.SlidingSizeForOffset(offset);

            return new CGRect(this.ResizesCenterView && offset < 0 ? 0 : offset, 0, sz.Width, sz.Height);
        }

        private CGSize SlidingSizeForOffset(nfloat offset) 
        {
            if (!this.ResizesCenterView)
            {
                return this.ReferenceBounds.Size;
            }

            offset = this.LimitOffset(offset);

            if (offset < 0) 
            {
                return new CGSize(this.CenterViewBounds.Size.Width + offset, this.CenterViewBounds.Size.Height);
            }

            return new CGSize(this.CenterViewBounds.Size.Width - offset, this.CenterViewBounds.Size.Height);
        }

        private void SetSlidingFrameForOffset(nfloat offset) 
        {
            this.offset = this.LimitOffset(offset);
            this.SlidingControllerView.Frame = this.SlidingRectForOffset(offset);

            if (this.Delegate != null)
            {
                this.Delegate.SlideOffsetChanged(this, this.offset);
            }
        }

        private void HideAppropriateSideViews() 
        {
            if (this.LeftController != null)
            {
                this.LeftController.View.Hidden = this.SlidingControllerView.Frame.GetMinX() <= 0;
            }

            if (this.RightController != null)
            {
                this.RightController.View.Hidden = this.SlidingControllerView.Frame.GetMaxX() >= this.ReferenceBounds.Size.Width;
            }
        }

        private void TryRemoveObserver(NSObject obj, string key)
        {
            try
            {
                obj.RemoveObserver(this, new NSString(key));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // yum yum
            }
        }

        private void ArrangeViewsAfterRotation() 
        {
            if (this.preRotationWidth <= 0) 
            {
                return;
            }

            var offset = this.SlidingControllerView.Frame.Location.X;

            if (this.ResizesCenterView && offset == 0) 
            {
                offset = offset + (this.preRotationCenterWidth - this.preRotationWidth);
            }
            
            if (this.RotationBehavior == ViewDeckRotationBehavior.KeepsLedgeSizes) 
            {
                if (offset > 0) 
                {
                    offset = this.ReferenceBounds.Size.Width - this.preRotationWidth + offset;
                }
                else if (offset < 0) 
                {
                    offset = offset + this.preRotationWidth - this.ReferenceBounds.Size.Width;
                }
            }
            else 
            {
                this.LeftLedge = this.LeftLedge + this.ReferenceBounds.Size.Width - this.preRotationWidth; 
                this.RightLedge = this.RightLedge + this.ReferenceBounds.Size.Width - this.preRotationWidth; 
                this.MaxLedge = this.MaxLedge + this.ReferenceBounds.Size.Width - this.preRotationWidth; 
            }

            this.SetSlidingFrameForOffset(offset);
            
            this.preRotationWidth = 0;
			this.preRotationSize = new CGSize ();
        }

        private void ShowCenterView(bool animated, Action<ViewDeckController> completed)
        {
            bool mustRunCompletion = completed != null;

            if (this.LeftController != null&& !this.LeftController.View.Hidden) 
            {
                this.CloseLeftView(animated, completed);
                mustRunCompletion = false;
            }
            
            if (this.RightController != null && !this.RightController.View.Hidden) 
            {
                this.CloseRightView(animated, completed);
                mustRunCompletion = false;
            }
            
            if (mustRunCompletion)
            {
                completed(this);
            }
        }

        private bool OpenLeftView(bool animated, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.OpenLeftView(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool OpenLeftView(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.LeftController == null || FloatEqual(this.SlidingControllerView.Frame.GetMinX(), this.LeftLedge)) return true;

            // check the delegate to allow opening
            if (callDelegate && this.Delegate != null && !this.Delegate.WillOpenLeftView(this, animated))
            {
                return false;
            }

            // also close the right view if it's open. Since the delegate can cancel the close, check the result.
            if (!this.CloseRightView(animated, options, callDelegate, completed))
            {
                return false;
            }

            UIView.Animate(OpenSlideDuration(animated), 0, options | UIViewAnimationOptions.LayoutSubviews | UIViewAnimationOptions.BeginFromCurrentState, () =>
            {
                this.LeftController.View.Hidden = false;
                this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width - this.LeftLedge);
				RefreshStatusBar();
                this.CenterViewHidden();
            }, () =>
            {
                if (completed != null) completed(this);
                if (callDelegate && this.Delegate != null) 
                {
                    this.Delegate.DidOpenLeftView(this, animated);
                }

				RefreshStatusBar();
            });
            
            return true;
        }

        private bool OpenLeftViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed) 
        {
            return this.OpenLeftViewBouncing(bounced, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }

        private bool OpenLeftViewBouncing(Action<ViewDeckController> bounced, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.LeftController == null || FloatEqual(this.SlidingControllerView.Frame.GetMinX(), this.LeftLedge)) return true;
            
            // check the delegate to allow opening
            if (callDelegate && this.Delegate != null && !this.Delegate.WillOpenLeftView(this, true))
            {
                return false;
            }

            // also close the right view if it's open. Since the delegate can cancel the close, check the result.
            if (!this.CloseRightView(true, options, callDelegate, completed))
            {
                return false;
            }
            
            // first open the view completely, run the block (to allow changes)
            UIView.Animate(OpenSlideDuration(true), 0, UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () =>
            {
                this.LeftController.View.Hidden = false;
                this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width);
				RefreshStatusBar();
            }, () => 
            {
                // run block if it's defined
                if (bounced != null) bounced(this);
                this.CenterViewHidden();
                
                // now slide the view back to the ledge position
                UIView.Animate(OpenSlideDuration(true), 0, options | UIViewAnimationOptions.LayoutSubviews | UIViewAnimationOptions.BeginFromCurrentState, () =>
                {
                    this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width - this.LeftLedge);
                }, () => 
                {
                    if (completed != null) 
                    {
                        completed(this);
                    }

                    if (callDelegate && this.Delegate != null)
                    {
                        this.Delegate.DidOpenLeftView(this, true);
                    }

					RefreshStatusBar();
                });
            });
            
            return true;
        }

        private bool CloseLeftView(bool animated, bool callDelegate, Action<ViewDeckController> completed) 
        {
            return this.CloseLeftView(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool CloseLeftView(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.LeftControllerIsClosed) return true;
            
            // check the delegate to allow closing
            if (callDelegate && this.Delegate != null && !this.Delegate.WillCloseLeftView(this, animated))
            {
                return false;
            }
            
            UIView.Animate(CloseSlideDuration(animated), 0, options | UIViewAnimationOptions.LayoutSubviews, () => 
            {
                this.SetSlidingFrameForOffset(0);
				RefreshStatusBar();
                this.CenterViewVisible();
            }, () =>  
            {
                this.HideAppropriateSideViews();
                if (completed != null) 
                {
                    completed(this);
                }

                if (callDelegate && this.Delegate != null) 
                {
                    this.Delegate.DidCloseLeftView(this, animated);
                    this.Delegate.DidShowCenterView(this, animated);
                }

				RefreshStatusBar();
            });
            
            return true;
        }

        private bool CloseLeftViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.LeftControllerIsClosed) return true;
            
            // check the delegate to allow closing
            if (callDelegate && this.Delegate != null && !this.Delegate.WillCloseLeftView(this, true))
            {
                return false;
            }
            
            // first open the view completely, run the block (to allow changes) and close it again.
            UIView.Animate(OpenSlideDuration(true), 0, UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () =>
            {
                this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width);
            }, () => 
            {
                // run block if it's defined
                if (bounced != null) bounced(this);

                if (callDelegate && this.Delegate != null)
                {
                    this.Delegate.DidBounceWithClosingController(this, this.LeftController);
                }

                UIView.Animate(CloseSlideDuration(true), 0, UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.LayoutSubviews, () => 
                {
                    this.SetSlidingFrameForOffset(0);
					RefreshStatusBar();
                    this.CenterViewVisible();
                } , () => 
                {
                    this.HideAppropriateSideViews();

                    if (completed != null) 
                    {
                        completed(this);
                    }

                    if (callDelegate && this.Delegate != null) 
                    {
                        this.Delegate.DidCloseLeftView(this, true);
                        this.Delegate.DidShowCenterView(this, true);
                    }

					RefreshStatusBar();
                });
            });
            
            return true;
        }

        private bool OpenRightView(bool animated, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.OpenRightView(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool OpenRightView(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.RightController == null || FloatEqual(this.SlidingControllerView.Frame.GetMaxX(), this.RightLedge)) return true;
            
            // check the delegate to allow opening
            if (callDelegate && this.Delegate != null && !this.Delegate.WillOpenRightView(this, animated))
            {
                return false;
            }

            // also close the left view if it's open. Since the delegate can cancel the close, check the result.
            if (!this.CloseLeftView(animated, options, callDelegate, completed))
            {
                return false;
            }
            
            UIView.Animate(OpenSlideDuration(animated), 0, options | UIViewAnimationOptions.LayoutSubviews, () => 
            {
                this.RightController.View.Hidden = false;
                this.SetSlidingFrameForOffset(this.RightLedge - this.ReferenceBounds.Size.Width);
				RefreshStatusBar();
                this.CenterViewHidden();
            }, () => 
            {
                if (completed != null) 
                {
                    completed(this);
                }

                if (callDelegate && this.Delegate != null)
                {
                    this.Delegate.DidOpenRightView(this, animated);
                }

				RefreshStatusBar();
            });

            return true;
        }

        private bool OpenRightViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.OpenRightViewBouncing(bounced, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }

        private bool OpenRightViewBouncing(Action<ViewDeckController> bounced, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.RightController == null || FloatEqual(this.SlidingControllerView.Frame.GetMinX(), this.RightLedge)) return true;
            
            // check the delegate to allow opening
            if (callDelegate && this.Delegate != null && !this.Delegate.WillOpenRightView(this, true))
            {
                return false;
            }

            // also close the right view if it's open. Since the delegate can cancel the close, check the result.
            if (!this.CloseLeftView(true, options, true, completed))
            {
                return false;
            }
            
            // first open the view completely, run the block (to allow changes)
            UIView.Animate(OpenSlideDuration(true), 0, UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () => 
            {
                this.RightController.View.Hidden = false;
                this.SetSlidingFrameForOffset(-this.ReferenceBounds.Size.Width);
            }, () =>  
            {
                // run block if it's defined
                if (bounced != null) bounced(this);
                this.CenterViewHidden();
                
                // now slide the view back to the ledge position
                UIView.Animate(OpenSlideDuration(true), 0, options | UIViewAnimationOptions.LayoutSubviews | UIViewAnimationOptions.BeginFromCurrentState, () => 
                {
                    this.SetSlidingFrameForOffset(this.RightLedge - this.ReferenceBounds.Size.Width);
                }, () => 
                {
                    if (completed != null) 
                    {
                        completed(this);
                    }

                    if (callDelegate && this.Delegate != null)
                    {
                        this.Delegate.DidOpenRightView(this, true);
                    }

					RefreshStatusBar();
                });
            });
            
            return true;
        }

        private bool CloseRightView(bool animated, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.CloseRightView(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool CloseRightView(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.RightControllerIsClosed) return true;
            
            // check the delegate to allow closing
            if (callDelegate && this.Delegate != null && !this.Delegate.WillCloseRightView(this, true))
            {
                return false;
            }
            
            UIView.Animate(CloseSlideDuration(animated), 0, options | UIViewAnimationOptions.LayoutSubviews, () => 
            {
                this.SetSlidingFrameForOffset(0);
                this.CenterViewVisible();
            }, () => 
            {
                if (completed != null) 
                {
                    completed(this);
                }

                this.HideAppropriateSideViews();
                if (callDelegate && this.Delegate != null) 
                {
                    this.Delegate.DidCloseRightView(this, animated);
                    this.Delegate.DidShowCenterView(this, animated);
                }

				RefreshStatusBar();
            });
            
            return true;
        }

        private bool CloseRightViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.RightControllerIsClosed) return true;
            
            // check the delegate to allow closing
            if (callDelegate && this.Delegate != null && !this.Delegate.WillCloseRightView(this, true))
            {
                return false;
            }
            
            UIView.Animate(OpenSlideDuration(true), 0,  UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () => 
            {
                this.SetSlidingFrameForOffset(-this.ReferenceBounds.Size.Width);
            }, () =>  
            {
                if (bounced != null) bounced(this);

                if (callDelegate && this.Delegate != null)
                {
                    this.Delegate.DidBounceWithClosingController(this, this.RightController);
                }

                UIView.Animate(CloseSlideDuration(true), 0, UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.LayoutSubviews, () => 
                {
                    this.SetSlidingFrameForOffset(0);
                    this.CenterViewVisible();
                }, () =>  
                {
                    this.HideAppropriateSideViews();
                    if (completed != null) 
                    {
                        completed(this);
                    }

                    if (callDelegate && this.Delegate != null)
                    {
                        this.Delegate.DidCloseRightView(this, true);
                        this.Delegate.DidShowCenterView(this, true);
                    }

					RefreshStatusBar();
                });
            });
            
            return true;
        }

        private static CGRect  RectangleFOffset(CGRect rect, nfloat dx, nfloat dy)
        {
            // todo: is this correct
            return rect.Inset(dx, dy);
        }

        private void CenterViewVisible()
        {
            this.RemovePanners();
            if (this.centerTapper != null) 
            {
                this.centerTapper.RemoveTarget(this, new Selector("centerTapped"), UIControlEvent.TouchUpInside);
                this.centerTapper.RemoveFromSuperview();
            }

            this.centerTapper = null;
            this.AddPanners();
        }

        private void CenterViewHidden() 
        {
            if (this.centerHiddenInteractivity == CenterHiddenInteractivity.UserInteractive) 
                return;
            
            this.RemovePanners();

            if (this.centerTapper == null) 
            {
                this.centerTapper =  new UIButton(UIButtonType.Custom);
                this.centerTapper.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                this.centerTapper.Frame = this.centerView.Bounds;
                this.centerView.AddSubview(this.centerTapper);
                this.centerTapper.AddTarget(this, new Selector("centerTapped"), UIControlEvent.TouchUpInside);
                this.centerTapper.BackgroundColor = UIColor.Clear;
            }

            this.centerTapper.Frame = this.centerView.Bounds;
            this.AddPanners();
        }

        [Export("centerTapped")]
        private void CenterTapped() 
        {
            // todo: handle additinal cases better
            if (this.centerHiddenInteractivity != CenterHiddenInteractivity.UserInteractive) 
            {
                if (this.LeftController != null && this.SlidingControllerView.Frame.GetMinX() > 0) 
                {
                    if (this.centerHiddenInteractivity == CenterHiddenInteractivity.NotUserInteractiveWithTapToClose) 
                    {
                        this.CloseLeftView();
                    }
                    else
                    {
                        this.CloseLeftViewBouncing(null);
                    }
                }

                if (this.RightController != null && this.SlidingControllerView.Frame.GetMinX() < 0) 
                {
                    if (this.centerHiddenInteractivity == CenterHiddenInteractivity.NotUserInteractiveWithTapToClose) 
                    {
                        this.CloseRightView();
                    }
                    else
                    {
                        this.CloseRightViewBouncing(null);
                    }
                }
                
            }
        }

        [Export("gestureRecognizerShouldBegin:")]
        private bool GestureRecognizerShouldBegin(UIGestureRecognizer gestureRecognizer)
        {
            var px = this.SlidingControllerView.Frame.Location.X;
            if (px != 0) return true;
                
            var x = this.LocationOfPanner((UIPanGestureRecognizer)gestureRecognizer);
            bool ok =  true;

            if (x > 0) 
            {
                if (this.Delegate != null)
                {
                    ok = this.Delegate.WillOpenLeftView(this, false);
                }

                if (!ok)
                {
                    this.CloseLeftView(false);
                }
            }
            else if (x < 0) 
            {
                if (this.Delegate != null)
                {
                    ok = this.Delegate.WillOpenRightView(this, false);
                }

                if (!ok)
                {
                    this.CloseRightView(false);
                }
            }
            
            return ok;
        }

        [Export("gestureRecognizer:shouldReceiveTouch:")]
        private bool GestureRecognizer(UIGestureRecognizer gestureRecognizer, UITouch touch) 
        {
            if (touch.View.GetType().IsSubclassOf(typeof(UISlider)))
            {
                return false;
            }

            this.panOrigin = this.SlidingControllerView.Frame.Location.X;
            return true;
        }

        private nfloat LocationOfPanner(UIPanGestureRecognizer panner) 
        {
            CGPoint pan = panner.TranslationInView(this.referenceView);
            var x = pan.X + this.panOrigin;

            if (this.LeftController == null) 
            {
				x = (nfloat)Math.Min(0, x);
            }

            if (this.RightController == null) 
            {
				x = (nfloat)Math.Max(0, x);
            }
            
            var w = this.ReferenceBounds.Size.Width;
			nfloat lx = (nfloat)(Math.Max(Math.Min(x, w - this.LeftLedge), -w + this.RightLedge));
            
            if (this.Elastic) 
            {
				nfloat dx = (nfloat)(Math.Abs(x) - Math.Abs(lx));

                if (dx > 0) 
                {
                    dx = dx / (float)Math.Log(dx + 1) * 2;
                    x = lx + (x < 0 ? -dx : dx);
                }
            }
            else 
            {
                x = lx;
            }
            
            return this.LimitOffset(x);
        }

        [Export("panned:")]
        private void Panned(UIPanGestureRecognizer panner) 
        {
            if (!this.Enabled) return;

            nfloat px = this.SlidingControllerView.Frame.Location.X;
            nfloat x = this.LocationOfPanner(panner);
            nfloat w = this.ReferenceBounds.Size.Width;

            Action didCloseSelector = null;
            Action didOpenSelector = null;
            
            // if we move over a boundary while dragging, ... 
            if (px <= 0 && x >= 0 && px != x) 
            {
                // ... then we need to check if the other side can open.
                if (px < 0) 
                {
                    bool canClose = true;

                    if (this.Delegate != null)
                    {
                        canClose = this.Delegate.WillCloseRightView(this, false);
                        didCloseSelector = () => this.Delegate.DidCloseRightView(this, false);
                    }

                    if (!canClose)
                    {
                        return;
                    }
                }

                if (x > 0) 
                {
                    bool canOpen = true;

                    if (this.Delegate != null)
                    {
                        canOpen = this.Delegate.WillOpenLeftView(this, false);
                        didOpenSelector = () => this.Delegate.DidOpenLeftView(this, false);
                    }

                    if (!canOpen) 
                    {
                        this.CloseRightView(false);
                        return;
                    }
                }
            }
            else if (px >= 0 && x <= 0 && px != x) 
            {
                if (px > 0) 
                {
                    bool canClose = true;

                    if (this.Delegate != null)
                    {
                        canClose = this.Delegate.WillCloseLeftView(this, false);
                        didCloseSelector = () => this.Delegate.DidCloseLeftView(this, false);
                     }

                    if (!canClose) 
                    {
                        return;
                    }

                }

                if (x < 0) 
                {
                    bool canOpen = true;

                    if (this.Delegate != null)
                    {
                        canOpen = this.Delegate.WillOpenRightView(this, false);
                        didOpenSelector = () => this.Delegate.DidOpenRightView(this, false);
                    }

                    if (!canOpen) 
                    {
                        this.CloseLeftView(false);
                        return;
                    }
                }
            }
            
            this.SetSlidingFrameForOffset(x);
            
            if (this.Delegate != null)
            {
                this.Delegate.DidPanToOffset(this, x);
            }
            
            if (panner.State == UIGestureRecognizerState.Ended) 
            {
                if (this.SlidingControllerView.Frame.Location.X == 0.0f) 
                {
                    this.CenterViewVisible();
                }
                else
                {
                    this.CenterViewHidden();
                }

                nfloat lw3 = (w - this.LeftLedge) / 3.0f;
                nfloat rw3 = (w - this.RightLedge) / 3.0f;
                nfloat velocity = panner.VelocityInView(this.referenceView).X;

                if (Math.Abs(velocity) < 500) 
                {
                    // small velocity, no movement
                    if (x >= w - this.LeftLedge - lw3) 
                    {
                        this.OpenLeftView(true, UIViewAnimationOptions.CurveEaseOut, false, null);
                    }
                    else if (x <= this.RightLedge + rw3 - w) 
                    {
                        this.OpenRightView(true, UIViewAnimationOptions.CurveEaseOut, false, null);
                    }
                    else
                    {
                        this.ShowCenterView(true);
                    }
                }
                else if (velocity < 0) 
                {
                    // swipe to the left
                    if (x < 0) 
                    {
                        this.OpenRightView(true, UIViewAnimationOptions.CurveEaseOut, true, null);
                    }
                    else 
                    {
                        this.ShowCenterView(true);
                    }
                }
                else if (velocity > 0) 
                {
                    // swipe to the right
                    if (x > 0) 
                    {
                        this.OpenLeftView(true, UIViewAnimationOptions.CurveEaseOut, true, null);
                    }
                    else 
                    {
                        this.ShowCenterView(true);
                    }
                }
            }
            else
            {
                this.HideAppropriateSideViews();
            }

            if (didCloseSelector != null)
            {
                didCloseSelector();
            }

            if (didOpenSelector != null)
            {
                didOpenSelector();
            }
        }

        private void AddPanner(UIView view) 
        {
            if (view == null) return;

            UIPanGestureRecognizer panner = new UIPanGestureRecognizer(this, new Selector("panned:"));

            panner.CancelsTouchesInView = true;
            panner.WeakDelegate = this;

            this.View.AddGestureRecognizer(panner);
            this.panners.Add(panner);
        }

        private void AddPanners() 
        {
            this.RemovePanners();
            
            switch (this.PanningMode) 
            {
                case ViewDeckPanningMode.NoPanning: 
                    break;
                    
                case ViewDeckPanningMode.FullViewPanning:
                    this.AddPanner(this.SlidingControllerView);

                    // also add to disabled center
                    if (this.centerTapper != null)
                    {
                        this.AddPanner(this.centerTapper);
                    }

                    // also add to navigationbar if present
                    if (this.NavigationController != null && !this.NavigationController.NavigationBarHidden) 
                    {
                        this.AddPanner(this.NavigationController.NavigationBar);
                    }

                    break;
                    
                case ViewDeckPanningMode.NavigationBarPanning:
                    if (this.NavigationController != null && !this.NavigationController.NavigationBarHidden) 
                    {
                        this.AddPanner(this.NavigationController.NavigationBar);
                    }
                    
                    if (this.CenterController.NavigationController != null && !this.CenterController.NavigationController.NavigationBarHidden) 
                    {
                        this.AddPanner(this.CenterController.NavigationController.NavigationBar);
                    }
                    
                    if (this.CenterController.GetType().IsSubclassOf(typeof(UINavigationController)) && !((UINavigationController)this.CenterController).NavigationBarHidden) 
                    {
                        this.AddPanner(((UINavigationController)this.CenterController).NavigationBar);
                    }

                    break;
                case ViewDeckPanningMode.PanningViewPanning:
                    if (this.PanningView != null) 
                    {
                        this.AddPanner(this.PanningView);
                    }

                    break;
            }
        }


        private void RemovePanners() 
        {
            foreach (var panner in this.panners) 
            {
                panner.View.RemoveGestureRecognizer(panner);
            }

            this.panners.Clear();
        }

        private void ApplySideController(ref UIViewController controllerStore, UIViewController newController, UIViewController otherController, 
                                             Action clearOtherController) 
        {
            Action<UIViewController> beforeBlock = (x) => {};
            Action<UIViewController, bool> afterBlock = (x, y) => {};

            if (this.viewAppeared) 
            {
                beforeBlock = (controller) => 
                {
                    controller.View.RemoveFromSuperview();
                };

                afterBlock = (controller, left) => 
                {
                    controller.View.Hidden = left ? this.SlidingControllerView.Frame.Location.X <= 0 : this.SlidingControllerView.Frame.Location.X >= 0;
                    controller.View.Frame = this.ReferenceBounds;
                    controller.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    if (this.slidingController != null)
                    {
                        this.referenceView.InsertSubviewBelow(controller.View, this.SlidingControllerView);
                    }
                    else
                    {
                        this.referenceView.AddSubview(controller.View);
                    }
                };
            }
            
            // start the transition
            if (controllerStore != null) 
            {
                controllerStore.WillMoveToParentViewController(null);
                if (newController == this.CenterController) 
                {
                    this.CenterController = null;
                }

                if (newController == otherController && clearOtherController != null) clearOtherController();

                beforeBlock(controllerStore);

//                controllerStore.setViewDeckController(null);
                controllerStore.RemoveFromParentViewController();
                controllerStore.DidMoveToParentViewController(null);
            }
            
            // make the switch
            if (controllerStore != newController) 
            {
                // todo: dispose II_RELEASE(*controllerStore);
                controllerStore = newController;
            }
            
            if (controllerStore != null) 
            {
                newController.WillMoveToParentViewController(null);
                newController.RemoveFromParentViewController();
                newController.DidMoveToParentViewController(null);
                
                // and finish the transition
                UIViewController parentController = (this.referenceView == this.View) ? this : this.GetGrandParent();
                if (parentController != null)
                {
                    parentController.AddChildViewController(controllerStore);
                }

//                controllerStore.setViewDeckController(this);

                afterBlock(controllerStore, controllerStore == this.LeftController);

                controllerStore.DidMoveToParentViewController(parentController);
            }
        }

        private UIViewController GetGrandParent()
        {
            if (this.ParentViewController != null)
            {
                return this.ParentViewController.ParentViewController;
            }

            return null;
        }

        private void ReapplySideController(UIViewController controllerStore) 
        {
            if (controllerStore != null)
            {
                this.ApplySideController(ref controllerStore, controllerStore, null, null);
            }
        }

        #region Property Setters

        /// <summary>
        /// Set the center controller
        /// </summary>
        private void SetCenterController(UIViewController centerController) 
        {
            if (this.CenterController == centerController) 
            {
                return;
            }

            Action<UIViewController> beforeBlock = (x) => {};
            Action<UIViewController> afterBlock = (x) => {};

            var currentFrame = this.ReferenceBounds;

            if (this.viewAppeared) 
            {
                beforeBlock = (controller) => 
                {
                    this.RestoreShadowToSlidingView();
                    this.RemovePanners();
                    controller.View.RemoveFromSuperview();
                    this.centerView.RemoveFromSuperview();
                };

                afterBlock = (controller) => 
                {
                    this.View.AddSubview(this.centerView);

//                    UINavigationController navController = centerController.GetType().IsSubclassOf(typeof(UINavigationController)) 
//                    ? (UINavigationController)centerController 
//                    : null;
//
//                    bool barHidden = false;
//                    if (navController != null && !navController.NavigationBarHidden) 
//                    {
//                        barHidden = true;
//                        navController.NavigationBarHidden = true;
//                    }
                    
                    this.SetSlidingAndReferenceViews();
                    controller.View.Frame = currentFrame;
                    controller.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    controller.View.Hidden = false;
                    this.centerView.AddSubview(controller.View);
                    
//                    if (barHidden) 
//                        navController.NavigationBarHidden = false;
                    
                    this.AddPanners();
                    this.ApplyShadowToSlidingView();
                };
            }
            
            // start the transition
            if (this.CenterController != null) 
            {
                currentFrame = this.CenterController.View.Frame;
                this.CenterController.WillMoveToParentViewController(null);

                if (centerController == this.LeftController) this.LeftController = null;
                if (centerController == this.RightController) this.RightController = null;

                beforeBlock(this.CenterController);

                this.TryRemoveObserver(this.CenterController, new NSString("title"));
                if (this.AutomaticallyUpdateTabBarItems) 
                {
                    this.TryRemoveObserver(this.CenterController, new NSString("tabBarItem.title"));
                    this.TryRemoveObserver(this.CenterController, new NSString("tabBarItem.image"));
                    this.TryRemoveObserver(this.CenterController, new NSString("hidesBottomBarWhenPushed"));
                }

// todo:                this.centerController.setViewDeckController(null);
                this.CenterController.RemoveFromParentViewController();
                
                this.CenterController.DidMoveToParentViewController(null);
                // todo: dispose ? II_RELEASE(_centerController);
            }
            
            // make the switch
            this._centerController = centerController;
            
            if (this.CenterController != null) 
            {
                // and finish the transition
                this.AddChildViewController(this.CenterController);

// todo:                this.centerController.setViewDeckController(this);
                this.CenterController.AddObserver(this, new NSString("title"), 0, IntPtr.Zero);

                this.Title = this.CenterController.Title ?? string.Empty;

                if (this.AutomaticallyUpdateTabBarItems) 
                {
                    this.CenterController.AddObserver(this, new NSString("tabBarItem.title"), 0, IntPtr.Zero);
                    this.CenterController.AddObserver(this, new NSString("tabBarItem.image"), 0, IntPtr.Zero);
                    this.CenterController.AddObserver(this, new NSString("hidesBottomBarWhenPushed"), 0, IntPtr.Zero);
                    
                    this.TabBarItem.Title = this.CenterController.TabBarItem.Title;
                    this.TabBarItem.Image = this.CenterController.TabBarItem.Image;
                    this.HidesBottomBarWhenPushed = this.CenterController.HidesBottomBarWhenPushed;
                }
                
                afterBlock(this.CenterController);

                this.CenterController.DidMoveToParentViewController(this);
            }    
        }

        /// <summary>
        /// </summary>
        private void SetRightLedge(nfloat rightLedge) 
        {
			var minLedge = (nfloat)Math.Min(this.ReferenceBounds.Size.Width, rightLedge);
			rightLedge = (nfloat)Math.Max(rightLedge, minLedge);

            if (this.viewAppeared && FloatEqual(this.SlidingControllerView.Frame.Location.X, this.RightLedge - this.ReferenceBounds.Size.Width)) 
            {
                if (rightLedge < this.RightLedge) 
                {
                    UIView.Animate(CloseSlideDuration(true), () =>
                    {
                        this.SetSlidingFrameForOffset(rightLedge - this.ReferenceBounds.Size.Width);
                    });
                }
                else if (rightLedge > this.RightLedge) 
                {
                    UIView.Animate(OpenSlideDuration(true),() =>
                    {
                        this.SetSlidingFrameForOffset(rightLedge - this.ReferenceBounds.Size.Width);
                    });
                }
            }

            this._rightLedge = rightLedge;
        }

        /// <summary>
        /// </summary>
        private void SetRightLedge(nfloat rightLedge, Action<bool> completion)
        {
			var minLedge = (nfloat)Math.Min(this.ReferenceBounds.Size.Width, rightLedge);
			rightLedge = (nfloat)Math.Max(rightLedge, minLedge);

            if (this.viewAppeared && FloatEqual(this.SlidingControllerView.Frame.Location.X, this.RightLedge - this.ReferenceBounds.Size.Width)) 
            {
                if (rightLedge < this.RightLedge) 
                {
                    UIView.Animate(CloseSlideDuration(true), () =>
                    {
                        this.SetSlidingFrameForOffset(rightLedge - this.ReferenceBounds.Size.Width);
                    }, () => completion(true));
                }
                else if (rightLedge > this.RightLedge) 
                {
                    UIView.Animate(OpenSlideDuration(true),() =>
                    {
                        this.SetSlidingFrameForOffset(rightLedge - this.ReferenceBounds.Size.Width);
                    }, () => completion(true));
                }
            }

            this._rightLedge = rightLedge;
        }

        /// <summary>
        /// </summary>
        private void SetLeftLedge(nfloat leftLedge) 
        {
			var minLedge = (nfloat)Math.Min(this.ReferenceBounds.Size.Width, leftLedge);
			leftLedge = (nfloat)Math.Max(leftLedge, minLedge);

            if (this.viewAppeared && FloatEqual(this.SlidingControllerView.Frame.Location.X, this.ReferenceBounds.Size.Width - this.LeftLedge)) 
            {
                if (leftLedge < this.LeftLedge) 
                {
                    UIView.Animate(CloseSlideDuration(true), () =>
                    {
                        this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width - leftLedge);
                    });
                }
                else if (leftLedge > this.LeftLedge) 
                {
                    UIView.Animate(OpenSlideDuration(true),() =>
                   {
                        this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width - leftLedge);
                    });
                }
            }

            this._leftLedge = leftLedge;
        }

        /// <summary>
        /// </summary>
        private void SetLeftLedge(nfloat leftLedge, Action<bool> completion)
        {
			nfloat minLedge = (nfloat)Math.Min(this.ReferenceBounds.Size.Width, leftLedge);
			leftLedge = (nfloat)Math.Max(leftLedge, minLedge);

            if (this.viewAppeared && FloatEqual(this.SlidingControllerView.Frame.Location.X, this.ReferenceBounds.Size.Width - this.LeftLedge)) 
            {
                if (leftLedge < this.LeftLedge) 
                {
                    UIView.Animate(CloseSlideDuration(true), () =>
                    {
                        this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width - leftLedge);
                    }, () => completion(true));
                }
                else if (leftLedge > this.LeftLedge) {
                    UIView.Animate(OpenSlideDuration(true),() =>
                    {
                        this.SetSlidingFrameForOffset(this.ReferenceBounds.Size.Width - leftLedge);
                    }, () => completion(true));
                }
            }

            this._leftLedge = leftLedge;
        }

        #endregion

        private bool SetSlidingAndReferenceViews() 
        {
            if (this.NavigationController != null && this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Integrated) 
            {
                if (this.NavigationController.View.Superview != null) 
                {
                    this.slidingController = this.NavigationController;
                    this.referenceView = this.NavigationController.View.Superview;
                    return true;
                }
            }
            else 
            {
                this.slidingController = this.CenterController;
                this.referenceView = this.View;
                return true;
            }
            
            return false;
        }

        private void RestoreShadowToSlidingView() 
        {
            UIView shadowedView = this.SlidingControllerView;
            if (shadowedView == null) return;
            
            shadowedView.Layer.ShadowRadius = this.originalShadowRadius;
			shadowedView.Layer.ShadowOpacity = (float)this.originalShadowOpacity;
            shadowedView.Layer.ShadowColor = this.originalShadowColor.CGColor; 
            shadowedView.Layer.ShadowOffset = this.originalShadowOffset;
            shadowedView.Layer.ShadowPath = this.originalShadowPath != null ? this.originalShadowPath.CGPath :  new CGPath();
        }

        private void ApplyShadowToSlidingView() 
        {
            UIView shadowedView = this.SlidingControllerView;
            if (shadowedView == null) return;
            
			if (shadowedView.Layer.ShadowPath == null)
				shadowedView.Layer.ShadowPath = new CGPath ();

            this.originalShadowRadius = shadowedView.Layer.ShadowRadius;
            this.originalShadowOpacity = shadowedView.Layer.ShadowOpacity;
            this.originalShadowColor = shadowedView.Layer.ShadowColor != null ? UIColor.FromCGColor(shadowedView.Layer.ShadowColor) : null;
            this.originalShadowOffset = shadowedView.Layer.ShadowOffset;

            this.originalShadowPath = shadowedView.Layer.ShadowPath.Handle != IntPtr.Zero ? UIBezierPath.FromPath(shadowedView.Layer.ShadowPath) : null;

            if (this.Delegate == null || !this.Delegate.ApplyShadow(this, shadowedView.Layer, this.ReferenceBounds))
            {
                shadowedView.Layer.MasksToBounds = false;
                shadowedView.Layer.ShadowRadius = 10;
                shadowedView.Layer.ShadowOpacity = 0.5f;
                shadowedView.Layer.ShadowColor = UIColor.Black.CGColor;
                shadowedView.Layer.ShadowOffset = CGSize.Empty;
                shadowedView.Layer.ShadowPath = UIBezierPath.FromRect(shadowedView.Bounds).CGPath;
            }
        }

		#region Status Bar

		private UIViewController ActiveViewController
		{
			get
			{
				UIViewController vc = CenterController;
				if (LeftController != null && LeftControllerIsOpen)
					vc = LeftController;
				if (RightController != null && RightControllerIsOpen)
					vc = RightController;
				return vc;
			}
		}

		public override UIStatusBarStyle PreferredStatusBarStyle ()
		{
			UIViewController vc = ActiveViewController;
			if (vc != null)
				return vc.PreferredStatusBarStyle ();
			return UIStatusBarStyle.Default;
		}

		public override bool PrefersStatusBarHidden ()
		{
			UIViewController vc = ActiveViewController;
			if (vc != null)
				return vc.PrefersStatusBarHidden();
			return false;
		}

		private void RefreshStatusBar()
		{
			Selector selector = new Selector ("setNeedsStatusBarAppearanceUpdate");
			if (RespondsToSelector(selector))
				SetNeedsStatusBarAppearanceUpdate ();
		}

		#endregion
    }
}

