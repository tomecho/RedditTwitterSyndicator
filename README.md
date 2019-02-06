# Components

## RedditPull
Use the reddit api to pull top posts at some defined interval and post them to the az storage table
## TwitterPush
At a defined interval read from our az storage table, look for entries that haven't been posted yet and post those to twitter using the twitter api
## TableCleanup
Clear out any posts from the table that have already been posted or are over 7 days olds