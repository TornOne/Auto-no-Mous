﻿using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

public class GameManager : MonoBehaviour {
	GameObject[] cars;
	bool started = false;
	public bool inGame;
	int generation = 1;

    public GameObject aiCarPrefab;
	public int nrOfCars;
	public TrackMaker trackMaker;
	public UIController UIController;

	System.Random rng = new System.Random();

	void Start() {
		cars = new GameObject[nrOfCars];
	}

	void Update() {
		foreach (var car in cars) {
			if (car != null && car.activeSelf) {
				return;
			}
		}

		if (started) {
			SpawnNewCars();
		}
	}

	public void EnterPlayMode() {
		generation = 1;
		inGame = true;
		ClearCars();
		SpawnCars();
	}

	public void LeavePlayMode() {
		inGame = false;
		started = false;
		ClearCars();
	}

	public void ClearCars() {
		for (var i = 0; i < cars.Length; i++) {
			if (cars[i] != null) {
				Destroy(cars[i]);
				cars[i] = null;
			}
		}
	}

	void SpawnCars() {
		for (var i = 0; i < nrOfCars; i++) {
			var car = Instantiate(aiCarPrefab);
			cars[i] = car;
		}

		started = true;
	}

	void SelectPair(CarController[] cars, out CarController car1, out CarController car2) {
		int choice1 = rng.Next(cars.Length);
		int choice2 = 0;
		do {
			choice2 = rng.Next(cars.Length);
		} while (choice2 == choice1);

		car1 = cars[choice1];
		car2 = cars[choice2];
	}

	CarController SelectOne(CarController[] cars) {
		return cars[rng.Next(cars.Length)];
	}

	const float Elite = 0.1f;
	const float Newbie = 0.2f;
	const float FullCross = 0.4f;
	const float LinearCross = 0.6f;

	void SpawnNewCars() {
		CarController[] lastCars = System.Array.ConvertAll(cars, car => car.GetComponent<CarController>());
		System.Array.Sort(lastCars, (car1, car2) => {
			int diff = car2.points - car1.points;
			if (diff != 0) {
				return diff;
			} else {
				if (car1.TotalTime == car2.TotalTime) {
					return 0;
				} else {
					return car1.TotalTime > car2.TotalTime ? 1 : -1;
				}
			}
		});
		System.Array.Resize(ref lastCars, Mathf.RoundToInt(Elite * 4 * nrOfCars));

		UIController.UpdateInfoPanel(generation++, lastCars[0]);

		ClearCars();
		SpawnCars();

		//Keep best cars
		for (int i = 0; i < Mathf.RoundToInt(Elite * nrOfCars); i++) {
			lastCars[i].GetComponent<NeuralNetwork>().GetNetwork(out Matrix<double>[] bestWeights, out Vector<double>[] bestBiases);
			cars[i].GetComponent<NeuralNetwork>().SetNetwork(bestWeights, bestBiases);
			cars[i].name = "Best car " + lastCars[i].points + "p " + lastCars[i].TotalTime + "t";
			cars[i].GetComponentInChildren<MeshRenderer>().material.color = Color.red;
		}

		//Generate new cars
		for (int i = Mathf.RoundToInt(Elite * nrOfCars); i < Mathf.RoundToInt(Newbie * nrOfCars); i++) {
			cars[i].GetComponent<NeuralNetwork>().SetRandom();
			cars[i].name = "Random restart";
			cars[i].GetComponentInChildren<MeshRenderer>().material.color = Color.HSVToRGB(0.2f, 1, 1);
		}

		//Full cross best cars
		for (int i = Mathf.RoundToInt(Newbie * nrOfCars); i < Mathf.RoundToInt(FullCross * nrOfCars); i++) {
			SelectPair(lastCars, out var selectedCar1, out var selectedCar2);

			NeuralNetwork.FullCrossover(selectedCar1.gameObject, selectedCar2.gameObject, out Matrix<double>[] weights, out Vector<double>[] biases);
			cars[i].GetComponent<NeuralNetwork>().SetNetwork(weights, biases);
			cars[i].name = "Full cross (" + selectedCar1.points + "p " + selectedCar1.TotalTime + "t) & (" + selectedCar2.points + "p " + selectedCar2.TotalTime + "t)";
			cars[i].GetComponentInChildren<MeshRenderer>().material.color = Color.HSVToRGB(0.4f, 1, 1);
		}

		//Linear cross best cars
		for (int i = Mathf.RoundToInt(FullCross * nrOfCars); i < Mathf.RoundToInt(LinearCross * nrOfCars); i++) {
			SelectPair(lastCars, out var selectedCar1, out var selectedCar2);

			NeuralNetwork.LinearCrossover(selectedCar1.gameObject, selectedCar2.gameObject, out Matrix<double>[] weights, out Vector<double>[] biases);
			cars[i].GetComponent<NeuralNetwork>().SetNetwork(weights, biases);
			cars[i].name = "Linear cross (" + selectedCar1.points + "p " + selectedCar1.TotalTime + "t) & (" + selectedCar2.points + "p " + selectedCar2.TotalTime + "t)";
			cars[i].GetComponentInChildren<MeshRenderer>().material.color = Color.HSVToRGB(0.6f, 1, 1);
		}

		//Mutate best cars
		for (int i = Mathf.RoundToInt(LinearCross * nrOfCars); i < cars.Length; i++) {
			var selectedCar = SelectOne(lastCars);

			NeuralNetwork.Mutate(selectedCar.gameObject, out Matrix<double>[] weights, out Vector<double>[] biases);
			cars[i].GetComponent<NeuralNetwork>().SetNetwork(weights, biases);
			cars[i].name = "Mutant " + selectedCar.points + "p " + selectedCar.TotalTime + "t";
			cars[i].GetComponentInChildren<MeshRenderer>().material.color = Color.HSVToRGB(0.8f, 1, 1);
		}
	}
}
