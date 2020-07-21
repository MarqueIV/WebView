﻿using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using NUnit.Framework;

namespace Tests {

    [TestFixture]
    public abstract class TestBase<T> where T : class, IDisposable, new() {

        protected virtual TimeSpan DefaultTimeout => TimeSpan.FromSeconds(5);

        private Window window;
        private T view;

        protected static string CurrentTestName => TestContext.CurrentContext.Test.Name;

        [OneTimeSetUp]
        protected void OneTimeSetUp() {
            if (Application.Current == null) {
               AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
            }
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown() {
            if (view != null) {
                view.Dispose();
            }
            window.Close();
        }

        [SetUp]
        protected void SetUp() {
            AvaloniaLocator.EnterScope();

            window = new Window {
                Title = "Running: " + CurrentTestName
            };

            window.Show();

            if (view == null) {
                view = CreateView();

                if (view != null) {
                    InitializeView();
                }

                window.Content = view;

                if (view != null) {
                    AfterInitializeView();
                }
            }
        }

        protected Window Window => window;

        protected virtual T CreateView() {
            return new T();
        }

        protected virtual void InitializeView() { }

        protected virtual void AfterInitializeView() { }

        [TearDown]
        protected void TearDown() {
            if (Debugger.IsAttached && TestContext.CurrentContext.Result.FailCount > 0) {
                ShowDebugConsole();
                WaitFor(() => false, TimeSpan.MaxValue);
                return;
            }
            if (view != null) {
                view.Dispose();
                view = null;
            }
            window.Content = null;
        }

        protected abstract void ShowDebugConsole();

        protected T TargetView {
            get { return view; }
        }

        public void WaitFor(Func<bool> predicate, string purpose = "") {
            WaitFor(predicate, DefaultTimeout, purpose);
        }

        public static void WaitFor(Func<bool> predicate, TimeSpan timeout, string purpose = "") {
            var start = DateTime.Now;
            while (!predicate() && (DateTime.Now - start) < timeout && Application.Current != null) {
                DoEvents();
            }
            var elapsed = DateTime.Now - start;
            if (!predicate()) {
                throw new TimeoutException("Timed out waiting for " + purpose);
            }
        }

        [DebuggerNonUserCode]
        protected static void DoEvents() {
            Dispatcher.UIThread.InvokeAsync(delegate { }, DispatcherPriority.Background).Wait();
            Thread.Sleep(1);
        }

        protected bool FailOnAsyncExceptions { get; set; } = !Debugger.IsAttached;

        protected void OnUnhandledAsyncException(WebViewControl.UnhandledAsyncExceptionEventArgs e) {
            if (FailOnAsyncExceptions) {
                Dispatcher.UIThread.InvokeAsync(new Action(() => {
                    Assert.Fail("An async exception ocurred: " + e.Exception.ToString());
                }));
            }
        }
    }
}
