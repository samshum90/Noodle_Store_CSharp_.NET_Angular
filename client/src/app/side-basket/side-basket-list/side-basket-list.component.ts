import { Component, EventEmitter, OnDestroy, OnInit } from '@angular/core';
import { MatSelectChange } from '@angular/material/select';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { take } from 'rxjs/operators';
import { Order } from 'src/app/_models/order';
import { OrderedProducts } from 'src/app/_models/orderedProducts';
import { Product } from 'src/app/_models/product';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';
import { BasketService } from 'src/app/_services/basket.service';
import { DataStorageService } from 'src/app/_services/data-storage.service';

@Component({
  selector: 'app-side-basket-list',
  templateUrl: './side-basket-list.component.html',
  styleUrls: ['./side-basket-list.component.scss']
})
export class SideBasketListComponent implements OnInit, OnDestroy {
  basket!: Order;
  subscription!: Subscription;
  displayedColumns: string[] = ['photo', 'name', 'quantity', 'price'];
  dataSource!: MatTableDataSource<any>;
  numbers: number[] = [
    0,
    1,
    2,
    3,
    4,
    5,
    6,
    7,
    8,
    9,
  ];
  newQty!: number;
  numberOfItems!: number;

  constructor(public basketService: BasketService, public dataStorageService: DataStorageService) {
  }

  ngOnInit(): void {
    this.loadBasket();
  }

  loadBasket(): void {
    this.subscription = this.basketService.basketChanged
      .subscribe(
        (basketChanged: Order) => {
          this.basket = basketChanged;
          this.dataSource = new MatTableDataSource(this.basket.orderedProducts);
        }
      );
    this.basket = this.basketService.getBasket();
    if (!!this.basket) {
      this.getTotalQty();
      this.dataSource = new MatTableDataSource(this.basket.orderedProducts);

    }
  }

  getTotalCost(): number {
    this.getTotalQty();
    return this.basket.orderedProducts.map(op => parseFloat(op.product.salePrice) * op.quantity)
      .reduce((acc, value) => acc + value, 0);
  }

  getTotalQty() {
    this.basketService.numOfItems.subscribe(
      (number: number) => {
        this.numberOfItems = number;
      }
    );
  }

  selectQty(event: MatSelectChange, od: OrderedProducts) {
    this.basketService.updateProduct(event.value, od).subscribe(() => {
      this.getTotalCost();
      this.dataSource = new MatTableDataSource(this.basket.orderedProducts);
    });

    // this.newQty = parseInt((event.target as HTMLSelectElement).value);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}

