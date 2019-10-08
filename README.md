# Intro
This is an application which pulls the top reddit posts and posts them to a twitter account.  It is written in c# targeting Microsoft Azure cloud services.  It uses Azure Functions to execute the code at certain intervals and it uses Azure Storage Tables to temporarily store the posts between functions.  It is highly performant and runs great in parallel because of it's disconnected nature, responsibilites are divided between the resources.

# Components

## RedditPull
Use the reddit api to pull top posts at some defined interval and post them to the az storage table
## TwitterPush
At a defined interval read from our az storage table, look for entries that haven't been posted yet and post those to twitter using the twitter api
## TableCleanup
Clear out any posts from the table that have already been posted or are over 7 days olds