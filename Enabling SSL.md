# Enabling SSL

## Configuring the server.

In order to enable SSL, please go to your config file, located under `configs/Impostor.Command.cfg`. Then, change `"UseSsl":false` to `"UseSsl":true`. Now, you must run the server once, for the certificates to be generated.  
The server is now configured to use SSL encryption.

You can either use an external certificate or a self-signed one (ImpostorHQ will automatically generate a self-signed certificate in case it is not provided one).

### Using an outside certificate (Recommended)

ImpostorHQ can use Let's Encrypt free certificates without much effort.  
Just use certbot's instructions to generate one (use the standalone authenticator) and copy it over to the server's root under the name of `antiHttps.pfx` (Only .pfx certificates are accepted).  
Any other **valid** certificate with the same name will be accepted!  
The certificate must be signed by a trusted company, otherwise the Web Sockets API will **NOT** work.

**Note: This process does not require any browser-side configuration, so it can be done just one time and will just work everywhere!**

### Using a self signed certificate (Not Recommended)

The ImpostorHQ server automatically generates a self-signed SSL certificate.  
Because it is not signed by a trusted company, it is not trusted by any browser *for now*. In that sense, the Web Sockets API will **NOT** work unless you get around that by installing the certificate in **each browser you use**. 

All the steps are described below. **Please note that this must be done each time you need to login from a new client.**

#### Configuring the browser.

##### Step 1: Obtaining the public key.

After you have configured and started the server, two new files will appear on your server's root: `antiHttps.pfx` and `add-to-browser.cer`. As the name suggests, you need to copy/download the `add-to-browser.cer` file to a folder of your liking.

##### Step 2: Installing the certificate.

##### Google Chrome.

Navigate to the following URL: `chrome://settings/security?search=certificate`. After that, scroll down and you should see something similar:

![docs/images/ssl-chrome1](docs/images/ssl-chrome1)

Click on the `Manage Certificates` button. A dialog should appear:

![docs/images/ssl-chrome2](docs/images/ssl-chrome2)

Click on import to launch the certificate wizard. Click next. You should be taken to the following form:

![docs/images/ssl-chrome3](docs/images/ssl-chrome3)

Click Browse, and browse for the `add-to-browser.cer` file. Select it, then click Next -> Next.

![docs/images/ssl-chrome4](docs/images/ssl-chrome4)

You are now ready to finish this step. Click Finish, then you're done!

![docs/images/ssl-chrome5](docs/images/ssl-chrome5)

##### Mozilla Firefox
(Images Needed)
Click on the icon representing three small horizontal bars at the top right of Firefox

Click on "Options (Windows) / Preferences (Mac)"

On the left, click on "Privacy and security"

Go down and click on "View certificates"

Click on "Your certificates"

Click on "Import"

In the new window select the .pfx file to install

Click on "Open"

## Step 3: Connecting to the dashboard.

After you have started the server and configured the browser, you are ready to connect to your server! To do so, just use your regular URL, but replace `http://` with `https://`. Your URL should now look something like:

 `https://example.com:22024/client.html`

You are ready to use the dashboard securely!
