import logo from './logo.svg';
import './App.css';
import MyThree from './graphics/ThreeExample.mjs';

function App() {
	console.log("App executed");
	return (
		< MyThree />
		// <div className="App">
		// 	<header className="App-header">
		// 		<img src={logo} className="App-logo" alt="logo" />
		// 		<p>
		// 		Edit <code>src/App.js</code> and save to reload.
		// 		</p>
		// 		<a
		// 		className="App-link"
		// 		href="https://reactjs.org"
		// 		target="_blank"
		// 		rel="noopener noreferrer"
		// 		>
		// 		{"Fuyck you"}
		// 		</a>
		// 	</header>
		// </div>
	);
}

export default App;
