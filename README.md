## What is it? ##

Extensions to easy the use of WPF / XAML.

Keep in mind that the code is very new, so it is not stable at all.


## How to use ##

Add the reference to the project and add

` xmlns:ex="http://schemas.pescuma.org/wpf-ex/2010/xaml" `

to your XAML.


## Features ##

The implemented extensions are:

### ex:Grid.Columns ###

Set the number of columns to your Grid and position controls based on the order they appear inside the grid.

For example:

```
<Grid ex:Grid.Columns="4">
    <Label Content="Test 1" />
    <Label Content="Test 2" Grid.ColumnSpan="2" Grid.RowSpan="2" />
    <Label Content="Test 3" />
    <Label Content="Test 4" />
    <Label Content="Test 5" />
    <Label Content="Test 6" />
    <Label Content="Test 7" />
    <Label Content="Test 8" />
</Grid>
```

or

```
<Grid ex:Grid.Columns="Auto,Auto,Auto,*">
    <Label Content="Test 1" />
    <Label Content="Test 2" Grid.ColumnSpan="2" Grid.RowSpan="2" />
    <Label Content="Test 3" />
    <Label Content="Test 4" />
    <Label Content="Test 5" />
    <Label Content="Test 6" />
    <Label Content="Test 7" />
    <Label Content="Test 8" />
</Grid>
```

For any column you can set its min and max width using `type|min?:max?`. For example:
  * `Auto|10:30` creates a column of type `Auto` with width between 10 and 30
  * `Auto|10:` creates a column of type `Auto` with min width 10
  * `Auto|:30` creates a column of type `Auto` with max width 30

If `ex:Grid.Rows` is not defined it will create rows of height `Auto`.



### ex:Grid.Rows ###

Set the number of rows and the row heights based on the row information of controls inside the grid. It works very well with `ex:Grid.Columns`.

You don't need to write all rows, you can use `...` in the row information that you want to be repeated for the missing definitions.

For example:

This will make the 1st row have height 40px, the 2nd and 3rd be `Auto` and the last one be `*`.
```
<Grid ex:Grid.Columns="2" ex:Grid.Rows="40,Auto...,*">
    <Label Content="Test 1" />
    <Label Content="Test 2" Grid.ColumnSpan="2" Grid.RowSpan="2" />
    <Label Content="Test 3" />
    <Label Content="Test 4" />
    <Label Content="Test 5" />
    <Label Content="Test 6" />
    <Label Content="Test 7" />
    <Label Content="Test 8" />
</Grid>
```

or

This will make the 1st row have height 40px, the 2nd be `*`. The 2nd row will have no controls in it.
```
<Grid ex:Grid.Columns="2" ex:Grid.Rows="40,Auto...,*">
    <Label Content="Test 1" />
</Grid>
```

For any row you can set its min and max height using `type|min?:max?`. For example:
  * `Auto|10:30` creates a row of type `Auto` with height between 10 and 30
  * `Auto|10:` creates a row of type `Auto` with min height 10
  * `Auto|:30` creates a row of type `Auto` with max height 30

If `ex:Grid.Columns` is not defined it will create one column of width `Auto`.



### ex:Grid.CellSpacing ###

Set a spacing between controls inside the grid. It changes the Margin of all controls.

For example:

```
<Grid ex:Grid.CellSpacing="20">
    <Label Content="Test 1" />
    <Label Content="Test 2" Grid.ColumnSpan="2" Grid.RowSpan="2" />
    <Label Content="Test 3" />
    <Label Content="Test 4" />
    <Label Content="Test 5" />
    <Label Content="Test 6" />
    <Label Content="Test 7" />
    <Label Content="Test 8" />
</Grid>
```






### ex:WrapPanel.CellSpacing ###

Set a spacing between controls inside the WrapPanel. It changes the Margin of all controls.

For example:

```
<WrapPanel ex:WrapPanel.CellSpacing="10">
    <Button Width="170" Content="B1" />
    <Button Width="100" Content="B2" />
    <Button Width="190" Content="B3" />
    <Button Width="100" Content="B4" />
    <Button Width="190" Content="B5" />
    <Button Width="150" Content="B6" />
    <Button Width="130" Content="B7" />
    <Button Width="10" Content="B8" />
    <Button Width="190" Content="B9" />
    <Button Width="170" Content="B10" />
</WrapPanel>
```






### ex:WrapPanel.Justify ###

Justiry controls in line or column inside the WrapPanel. It changes the Margin of all controls, but only in needed direction.

It works well with `ex:WrapPanel.CellSpacing`. In this case all margins are changed.

For example:

```
<WrapPanel ex:WrapPanel.Justify="True">
    <Button Width="170" Content="B1" />
    <Button Width="100" Content="B2" />
    <Button Width="190" Content="B3" />
    <Button Width="100" Content="B4" />
    <Button Width="190" Content="B5" />
    <Button Width="150" Content="B6" />
    <Button Width="130" Content="B7" />
    <Button Width="10" Content="B8" />
    <Button Width="190" Content="B9" />
    <Button Width="170" Content="B10" />
</WrapPanel>
```






## Changelog ##

```
0.6
 + Allows multipliers in *
 + Allow to define min and/or max size using |min?:max?
 + Min and max use CellSpacing to try to make it consistent
 + If only rows are defined than a default column of type Auto will be created

0.5
 + ex:WrapPanel.Justify
 + Signed assembly

0.4
 + ex:WrapPanel.CellSpacing

0.3
 + ex:Grid.Rows

0.2
 + ex:Grid.CellSpacing

0.1
 + ex:Grid.Columns
```




<br>
<br>
<br>

<a href='https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=7E6JWPNVGWJ4Q&lc=BR&item_name=pescuma&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted'><img src='https://www.paypal.com/en_US/i/btn/btn_donate_LG.gif' /></a>