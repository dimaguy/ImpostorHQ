# Enabling SSL

## 1. Configuring the server.

In order to enable SSL, please go to your config file, located under `configs/Impostor.Command.cfg`. Then, change `"UseSsl":false` to `"UseSsl":true`. Now, you must run the server once, for the certificates to be generated.

That is all. The server is now configured to use encryption.

## 2. Configuring the browser.

The ImpostorHQ server automatically generates a self-signed SSL certificate. Because it is not signed by a trusted company, it is not trusted by the browsers. In that sense, the Web Sockets API will NOT work. To get around that, one must install the certificate in their browser.

All the steps are described below. Please note that this is a one-time action, which makes this process very easy.

### Step 1: Obtaining the public key.

After you have configured and started the server, two new files will appear on your server's root: `antiHttps.pfx` and `add-to-browser.cer`. As the name suggests, you need to copy/download the `add-to-browser.cer` file to a folder of your liking.

Instructions for more browsers will be added shortly.

### Step 2: Installing the certificate.

##### Google chrome.

Navigate to the following URL: chrome://settings/security?search=certificate. After that, scroll down and you should see something similar:

![https://femto.pw/7tt6](https://femto.pw/7tt6)

Click on the `Manage Certificates` button. A dialog should appear:

![https://femto.pw/x6td](https://femto.pw/x6td)

Click on import to launch the certificate wizard. Click next. You should be taken to the following form:

![https://femto.pw/5kmk](https://femto.pw/5kmk)

Click Browse, and browse for the `add-to-browser.cer` file. Select it, then click Next -> Next.

![https://femto.pw/uem6](https://femto.pw/uem6)

You are now ready to finish this step. Click Finish, then you're done!

![https://femto.pw/32i5](https://femto.pw/32i5)



## Step 3: Connecting to the dashboard.

After you have started the server and configured the browser, you are ready to connect to your server! To do so, just use your regular URL, but replace `http://` with `https://`. Your URL should now look something like:

 `https://the-great-exterminator.who:22024/client.html`

You are ready to use the dashboard securely!
