from flask import Flask, render_template, request, jsonify
import redis
import json

app = Flask(__name__)
redis_client = redis.Redis(host='redis', port=6379, db=0)

@app.route('/')
def index():
    return '''
    <html>
        <head>
            <title>Voting App</title>
            <style>
                body { font-family: Arial; text-align: center; padding: 50px; }
                button { padding: 15px 30px; margin: 10px; font-size: 18px; cursor: pointer; }
                .cat { background-color: #ff6b6b; color: white; }
                .dog { background-color: #4ecdc4; color: white; }
            </style>
        </head>
        <body>
            <h1>Vote for your favorite!</h1>
            <form action="/vote" method="post">
                <button type="submit" name="vote" value="cat" class="cat">🐱 Cat</button>
                <button type="submit" name="vote" value="dog" class="dog">🐶 Dog</button>
            </form>
            <p><a href="http://192.168.56.10:3000/">View Live Results</a></p>
        </body>
    </html>
    '''

@app.route('/vote', methods=['POST'])
def vote():
    vote = request.form['vote']
    if vote in ['cat', 'dog']:
        data = json.dumps({'vote': vote, 'voter_id': request.remote_addr})
        redis_client.rpush('votes', data)
        return f'<h1>Voted for {vote}!</h1><a href="/">Vote again!</a> / <a href="http://192.168.56.10:3000/">Results</a>'
    return 'Invalid vote', 400

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)
