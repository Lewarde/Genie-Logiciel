   M S B A M L   `   `   `  ����  EasySaveV2corrigé#    EasySave.Wpf.Views.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   $��Segoe UI)�5   	   $��12(�6   
�������  � WindowStartupLocation$  CenterScreen=�5	   	   $	��580��5
   	   $	��400��6   $	��580��6"   $	��400��61   .��5      � $��10q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� -   5         ��Name$ labelJobsHeader��5      $��0��6-   $��16(�6:   $
��Bold"�6H   $��0,0,0,10q�6Z   � 5   
   $��1��6    � ColumnDefinitions 5      �� 5      $��3* �6#   �� 5      $��* �6#   .+�5#      �� -   5         ��Name$ listBoxBackupJobs��5#      $��0��61   $��0,0,10,0q�6A    �� DisplayMemberPath$ DisplayMember��5%      ��5$      �� *
BackupJobs+ �� SelectedItem 5&      �� *SelectedBackupJob+ �� Mode$ TwoWay=�5'      �� 5)      $��1��6   $	��Top=�6)   .+�5*      �� -   5         ��Name$ buttonAddJob��5*      $��35��6/   $��0,0,0,10q�6;   
����İ�{�5+   5   �� *AreControlsEnabled+�� 5,      -   5        $ buttonEditJob��5,      $��35��60   $��0,0,0,10q�6<   
����İ�{�5-   5   �� *AreControlsEnabled+�� 5.      -   5        $ buttonDeleteJob��5.      $��35��62   $��0,0,0,10q�6>   
���\\��
�������{�5/   C   �� *AreControlsEnabled+50      51      �� 53   
   $��2��6   	 �� Orientation$	 
Horizontal=�6"   $��	0,10,0,10q�6;   .+�54      �� -   5        $ buttonExecuteSelected��54      $��Execute Selected��64   $��35��6O   $	��150��6[   $��0,0,10,0q�6g   
���q�<�
�������{�56      �� *AreControlsEnabled+�� 57      -   5        $ buttonExecuteAll��57      $��Execute All��6/   $��35��6E   $	��150��6Q   
���q�<�
�������{�59      �� *AreControlsEnabled+5:      �� 5<   
   $��3��6   $	 
Horizontal=�6"   $��	0,10,0,10q�6;   $��Center=�6N   .+�5=      �� -   5        $ labelLanguage��5=      $��Center=�6/   $��0,0,5,0q�6J   �� 5>      -	   5        
 ��Name$
 comboBoxLanguage��5>      $	��120��61   $��0,0,20,0q�6=   $��Center=�6O   ��5?      �� *AvailableLanguages+ 5@      �� *CurrentLanguage+$ TwoWay=�{�5A      �� *AreControlsEnabled+�� 5C      -
   5        $ labelLogFormat��5C      $��Center=�60   $��0,0,5,0q�6K   �� 5D      -   5        $
 comboBoxLogFormat��5D      $	��120��62   $��Center=�6>   ��5E      �� *AvailableLogFormats+ 5F      �� *CurrentLogFormat+$ TwoWay=�{�5G      �� *AreControlsEnabled+5H      �� 5J   
   $��4��6   $��Bottom=�6!   .@�5K      �� .��5L      �� ��6   �� *StatusMessage+5M      �� 5N      $��Right=�6   .��5O      � $	��100��6J   $��18��6V    � Value 6   �� *CurrentProgressPercentage+y�5P      �� *IsExecutingBackup+    BooleanToVisibilityConverter �� 	Converter# [  5Q      5R      5S       � 	Resources 5T      ��%   &	        �� 5U   
   6'   5V      5W      