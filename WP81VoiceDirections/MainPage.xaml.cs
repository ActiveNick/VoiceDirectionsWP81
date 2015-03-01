//==========================================================================
//
// Author:  Nick Landry
// Title:   Senior Technical Evangelist - Microsoft US DX - NY Metro
// Twitter: @ActiveNick
// Blog:    www.AgeofMobility.com
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Disclaimer: Portions of this code may been simplified to demonstrate
// useful application development techniques and enhance readability.
// As such they may not necessarily reflect best practices in enterprise 
// development, and/or may not include all required safeguards.
// 
// This code and information are provided "as is" without warranty of any
// kind, either expressed or implied, including but not limited to the
// implied warranties of merchantability and/or fitness for a particular
// purpose.
//
// To learn more about Universal Windows app development using Cortana
// and the Speech SDK, watch the full-day course for free on
// Microsoft Virtual Acdemy (MVA) at http://aka.ms/cortanamva
//
//==========================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace WP81VoiceDirections
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // The object for controlling the speech synthesis engine (voice).
        SpeechSynthesizer synthesizer;
        SpeechRecognizer recognizer;

        // The media object for controlling and playing audio.
        MediaElement mediaplayer;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                // Create the speech recognizer and speech synthesizer objects. 
                if (this.synthesizer == null)
                {
                    synthesizer = new SpeechSynthesizer();

                    //Retrieve the first female voice
                    synthesizer.Voice = SpeechSynthesizer.AllVoices
                        .First(i => (i.Gender == VoiceGender.Female && i.Description.Contains("United States")));

                    mediaplayer = new MediaElement();
                }
                if (this.recognizer == null)
                {
                    recognizer = new SpeechRecognizer();
                }
                // Add a grammar file constraint to the recognizer.
                var storageFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///TextAdventure.grxml"));
                var grammarFileConstraint = new Windows.Media.SpeechRecognition.SpeechRecognitionGrammarFileConstraint(storageFile, "GameDirections");

                //recognizer.UIOptions.ExampleText = @"Ex. 'go north', 'open the mailbox'";
                recognizer.Constraints.Add(grammarFileConstraint);

                // Compile the constraint.
                await recognizer.CompileConstraintsAsync();
            }
            catch (Exception err)
            {
                txtResult.Text = err.ToString();
            }
        }

        private async void btnRecognitionClick(object sender, RoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    this.btnRecognition.Content = "Listening...";
                    txtResult.Text = String.Empty;
                });

            try
            {
                // Perform speech recognition.  
                SpeechRecognitionResult speechRecognitionResult = await recognizer.RecognizeAsync();

                // Check the confidence level of the speech recognition attempt.
                if ((speechRecognitionResult.Confidence == SpeechRecognitionConfidence.Low) ||
                    (speechRecognitionResult.Confidence == SpeechRecognitionConfidence.Rejected))
                {
                    // If the confidence level of the speech recognition attempt is low, 
                    // ask the user to try again.
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            txtResult.Text = "Not sure what you said, please try again.";
                            ReadText("Not sure what you said, please try again");
                        });                  
                }
                else
                {
                    // Tell the user the photo is changing by updating
                    // the TextBox control and by using text-to-speech (TTS). 
                    string feedback = "";
                    if (speechRecognitionResult.Confidence == SpeechRecognitionConfidence.High)
                    {
                        feedback = "You said: \n\n" + speechRecognitionResult.Text;
                    }
                    else
                    {
                        // We use different feedback if the confidence is only medium to
                        // inform the user there is a chance of error in accuracy
                        feedback = "I think you said: \n\n" + speechRecognitionResult.Text;
                    }
                    
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            txtResult.Text = feedback;
                            ReadText(feedback);
                            this.btnRecognition.Content = "Start Speech Recognition";
                        });
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                // Ignore the cancellation exception of the recoOperation.
            }
            catch (Exception exception)
            {
                // Handle the speech privacy policy error.
                const uint HResultPrivacyStatementDeclined = 0x80045509;

                if ((uint)exception.HResult == HResultPrivacyStatementDeclined)
                {
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var messageDialog = new Windows.UI.Popups.MessageDialog(
                                "You must accept the speech privacy policy to continue.", "Speech Exception");
                            messageDialog.ShowAsync().GetResults();
                            this.btnRecognition.Content = "Start Speech Recognition";
                        });
                }
                else
                {
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            txtResult.Text = exception.Message;
                        });
                }
            }
        }

        private async void ReadText(string mytext)
        {
            //Reminder: You need to enable the Microphone capabilitiy in Windows Phone projects
            //Reminder: Add this namespace in your using statements
            //using Windows.Media.SpeechSynthesis;

            // Generate the audio stream from plain text.
            SpeechSynthesisStream stream = await synthesizer.SynthesizeTextToStreamAsync(mytext);

            // Send the stream to the media object.
            mediaplayer.SetSource(stream, stream.ContentType);
            mediaplayer.Play();
        }
    }
}
