# ADBSrv
Amazon Dash Button Service

A niet little Windows-Service to detect the button-pushed-event from an AmazonDashButton

Parts of the code are from user "Youresam" https://github.com/youresam/Dash-Button.

The Service can be configured like this:

There are 2 config-files:

1) ADBSrv.exe.config:

Inside this config you need to enter the MAC-Address of the Dashbutton you like to use.
Also you can set the following things:

-InterfaceIndex (default is 0)
-DuplicateIgnoreInterval (the time you have to wait until the button-event is working again)
-LogginPath (Make sure to just set the path to some folder and not to a file)
-DebugLogging

To set up the dashbuttons inside this config, you simply need to add the following tag:

<setting name="DashMac1" serializeAs="String">
  <value>MAC-ADDRESS with : </value>
</setting>

The name has to start with the word "Dash" and needs to end with a continous number. So if you add a forth button to the config the name
should be "DashMac4".

2) ButtonConfig.xml

In this config you set up the DLLs and used Class- and Method-name which is being executed when the butto-press was detected.

If you want to add a new button to the config file, you need to add a whole button-tag inside the "buttons" tag.
So if we go with the example from above, you would add the following tag inside "buttons":

<button4>
  <name>Call me what ever you like</name>
  <MACaddress>AC:63:BE:03:D1:34</MACaddress>
  <DLLPath>C:\Temp\test.dll</DLLPath>
  <ClassName>TestClass</ClassName>
  <MethodName>HelloWorld</MethodName>
</button4>

After these steps, you should be good to go.
Let me know if something is unclear of if you struggling to get this project/service running.
