﻿using System.Windows;

namespace TextFilter
{
    public partial class OptionsDialog : Window
    {
        #region Public Constructors
        public enum OptionsDialogResult
        {
            unknown,
            apply,
            cancel,
            edit,
            register,
            reset,
            save,
            unregister
        }

        private OptionsDialogResult _dialogResult;
        public OptionsDialog()
        {
            
            Owner = Application.Current.MainWindow;
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Methods

        public void Disable()
        {
            this.Hide();
            this.Close();
        }

        public OptionsDialogResult WaitForResult()
        {
            this.ShowDialog();
            return _dialogResult;
        }

        #endregion Public Methods

        #region Private Methods

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.cancel;
            Disable();
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.edit;
            Disable();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.save;
            Disable();
        }

        private void buttonRestart_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.apply;
            Disable();
        }

        #endregion Private Methods

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.reset;
            Disable();
        }

        private void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.register;
            Disable();
        }

        private void buttonUnRegister_Click(object sender, RoutedEventArgs e)
        {
            _dialogResult = OptionsDialogResult.unregister;
            Disable();
        }
    }
}