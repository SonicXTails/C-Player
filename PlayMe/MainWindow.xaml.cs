using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace PlayMe
{
    public partial class MainWindow : Window
    {
        private string[] audioFiles;
        private string[] shuffledAudioFiles;
        private bool isShuffled = false;
        private int currentTrackIndex = 0;
        private bool isPlaying = false;
        private TimeSpan currentPosition;
        private bool isRepeatModeOn = false;
        private Thread playbackThread;
        public List<string> listeningHistory = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            media.MediaOpened += media_MediaOpened;
            Slider_for_volume.ValueChanged += VolumeSlider_ValueChanged;

            Slider_for_volume.Value = 0.2;
            media.Volume = Slider_for_volume.Value;

            List_with_music.SelectionChanged += List_with_music_SelectionChanged;
        }

        // Методы для работы с папкой с музыкой
        private void Open_folder_with_music_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Выберите папку с музыкой"
            };

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                string selectedFolder = dialog.FileName;
                audioFiles = Directory.GetFiles(selectedFolder, "*.*", SearchOption.TopDirectoryOnly);
                List_with_music.Items.Clear();

                if (isShuffled)
                {
                    shuffledAudioFiles = audioFiles.OrderBy(x => Guid.NewGuid()).ToArray(); // Обновление shuffledAudioFiles
                    foreach (string file in shuffledAudioFiles)
                    {
                        List_with_music.Items.Add(Path.GetFileName(file));
                    }
                }
                else
                {
                    foreach (string file in audioFiles)
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (extension == ".mp3" || extension == ".wav" || extension == ".ogg" || extension == ".m4a")
                        {
                            List_with_music.Items.Add(Path.GetFileName(file));
                        }
                    }
                }

                if (List_with_music.Items.Count > 0)
                {
                    currentTrackIndex = 0;
                    PlayAudio();
                }
                else
                {
                    MessageBox.Show("В данной папке нет аудиофайлов\nПопробуйте другую директорию");
                }
            }
        }

        private void Current_Track_In_List_Box()
        {
            List_with_music.SelectedIndex = currentTrackIndex;
            List_with_music.ScrollIntoView(List_with_music.SelectedItem);
        }
        private void HighlightCurrentTrack()
        {
            List_with_music.SelectedIndex = currentTrackIndex;
            List_with_music.ScrollIntoView(List_with_music.SelectedItem);
        }

        private void Random_Click(object sender, RoutedEventArgs e)
        {
            isShuffled = !isShuffled;

            if (isShuffled)
            {
                shuffledAudioFiles = audioFiles.OrderBy(x => Guid.NewGuid()).ToArray();
            }

            UpdatePlaylist();
            PlayAudio();
        }

        private void UpdatePlaylist()
        {
            List_with_music.Items.Clear();

            if (isShuffled)
            {
                foreach (string file in shuffledAudioFiles)
                {
                    List_with_music.Items.Add(Path.GetFileName(file));
                }
            }
            else
            {
                foreach (string file in audioFiles)
                {
                    string extension = Path.GetExtension(file).ToLower();
                    if (extension == ".mp3" || extension == ".wav" || extension == ".ogg" || extension == ".m4a")
                    {
                        List_with_music.Items.Add(Path.GetFileName(file));
                    }
                }
            }

            Current_Track_In_List_Box();
        }




        // Методы для управления плеером
        private void PlayAudio()
        {
            if (List_with_music.Items.Count > 0)
            {
                string currentAudioFile = isShuffled ? shuffledAudioFiles[currentTrackIndex] : audioFiles[currentTrackIndex];
                media.Source = new Uri(currentAudioFile);
                media.Play();
                isPlaying = true;

                HighlightCurrentTrack(); // Выделение текущего трека в списке
                listeningHistory.Add(Path.GetFileName(currentAudioFile));
            }
        }

        private void media_MediaOpened(object sender, RoutedEventArgs e)
        {

            Slider_for_duration.Maximum = media.NaturalDuration.TimeSpan.TotalSeconds;

            playbackThread = new Thread(new ThreadStart(Update_Playback_Info));
            playbackThread.IsBackground = true;
            playbackThread.Start();

            media.MediaEnded += Media_MediaEnded_NextTrack;

        }

        private void Media_MediaEnded_NextTrack(object sender, RoutedEventArgs e)
        {
            PlayNextTrack();
        }

        private void Play_Pause_Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                currentPosition = media.Position;
                media.Pause();
                isPlaying = false;
            }
            else
            {
                media.Position = currentPosition;
                media.Play();
                isPlaying = true;
            }
        }

        private void Pre_tarck_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                PlayPreviousTrack();
            });
        }

        private void PlayPreviousTrack()
        {
            List_with_music.SelectedIndex = -1;

            if (currentTrackIndex > 0)
            {
                currentTrackIndex--;
            }
            else
            {
                currentTrackIndex = isShuffled ? shuffledAudioFiles.Length - 1 : audioFiles.Length - 1;
            }

            media.Position = TimeSpan.Zero;
            PlayAudio();

            HighlightCurrentTrack(); // Выделение текущего трека в списке
        }

        private void Next_Track_Click(object sender, RoutedEventArgs e)
        {
            PlayNextTrack();
        }

        private void PlayNextTrack()
        {
            // Снятие выделения с предыдущего трека
            List_with_music.SelectedIndex = -1;

            if (isRepeatModeOn)
            {
                media.Position = TimeSpan.Zero;
                media.Play();
            }
            else
            {
                currentTrackIndex++;

                if (currentTrackIndex >= (isShuffled ? shuffledAudioFiles.Length : audioFiles.Length))
                {
                    currentTrackIndex = 0;
                }

                media.Position = TimeSpan.Zero;
                PlayAudio();

                HighlightCurrentTrack(); // Выделение текущего трека в списке
            }
        }


        private void Repeat_Button_Click(object sender, RoutedEventArgs e)
        {
            isRepeatModeOn = !isRepeatModeOn;

            if (isRepeatModeOn)
            {
                media.MediaEnded += Media_MediaEnded_Repeat;
            }
            else
            {
                media.MediaEnded -= Media_MediaEnded_Repeat;
            }
        }

        private void Media_MediaEnded_Repeat(object sender, RoutedEventArgs e)
        {
            if (isRepeatModeOn)
            {
                media.Position = TimeSpan.Zero;
                media.Play();
            }
        }

        private void Next_Teack(object sender, RoutedEventArgs e)
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                Slider_for_duration.Maximum = media.NaturalDuration.TimeSpan.TotalSeconds;

                if (playbackThread != null && playbackThread.IsAlive)
                {
                    playbackThread.Abort();
                }

                playbackThread = new Thread(new ThreadStart(Update_Playback_Info));
                playbackThread.IsBackground = true;
                playbackThread.Start();
            }
        }

        private void Update_Playback_Info()
        {
            while (true)
            {
                Dispatcher.Invoke(() =>
                {
                    if (media.NaturalDuration.HasTimeSpan)
                    {
                        All_Time.Text = media.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                        Time_Of_Playing.Text = media.Position.ToString(@"mm\:ss");
                        Slider_for_duration.Value = media.Position.TotalSeconds;
                    }
                });

                Thread.Sleep(1000);
            }
        }

        private void Slider_for_duration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            media.Position = TimeSpan.FromSeconds(Slider_for_duration.Value);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (media != null)
            {
                media.Volume = Slider_for_volume.Value;
            }
        }

        private void List_with_music_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (List_with_music.SelectedIndex != -1)
            {
                currentTrackIndex = List_with_music.SelectedIndex;
                PlayAudio();
            }
        }




        // Методы для работы с историей прослушивания
        private void History_Click(object sender, RoutedEventArgs e)
        {
            History_Window historyWindow = new History_Window(listeningHistory);
            historyWindow.ShowDialog();
        }

        public void PlaySelectedTrack(string trackName)
        {
            int index = -1;
            for (int i = 0; i < audioFiles.Length; i++)
            {
                if (Path.GetFileName(audioFiles[i]) == trackName)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                currentTrackIndex = index;
                PlayAudio();
            }
        }
    }
}